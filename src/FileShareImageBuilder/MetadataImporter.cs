using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Spectre.Console;
using System.Data;
using UKHO.ADDS.Search.Configuration;

namespace FileShareImageBuilder
{
    public class MetadataImporter
    {
        private readonly string _sourceConnectionString;

        public MetadataImporter(SqlConnection targetDatabase)
        {
            _sourceConnectionString = ConfigurationReader.GetSourceDatabaseConnectionString();
            _ = targetDatabase;
        }

        public async Task ImportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var targetConnectionString = ConfigurationReader.GetTargetDatabaseConnectionString(StorageNames.FileShareEmulatorDatabase);

                var bacpacDirectory = ConfigurationReader.GetDataImagePath();
                Directory.CreateDirectory(bacpacDirectory);
                var bacpacPath = Path.Combine(bacpacDirectory, "metadata.bacpac");
                await ExportAndImportBacpacAsync(_sourceConnectionString, targetConnectionString, bacpacPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
                throw;
            }
        }

        private static async Task ExportAndImportBacpacAsync(
            string sourceConnectionString,
            string targetConnectionString,
            string bacpacPath,
            CancellationToken cancellationToken)
        {
            if (File.Exists(bacpacPath))
            {
                File.Delete(bacpacPath);
            }

            var sourceDbName = await GetDatabaseNameAsync(sourceConnectionString, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(sourceDbName))
            {
                throw new InvalidOperationException("Could not determine source database name.");
            }

            var targetDbName = await GetDatabaseNameAsync(targetConnectionString, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(targetDbName, StorageNames.FileShareEmulatorDatabase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Refusing to import: connected to unexpected target database '{targetDbName}'. Expected '{StorageNames.FileShareEmulatorDatabase}'.");
            }

            AnsiConsole.MarkupLine($"[yellow]Exporting bacpac from[/] [bold]{Markup.Escape(sourceDbName)}[/]...");
            var exportService = new DacServices(sourceConnectionString);
            exportService.ProgressChanged += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Message))
                {
                    AnsiConsole.MarkupLine($"[grey]{Markup.Escape(args.Message)}[/]");
                }
            };
            await Task.Run(() => exportService.ExportBacpac(bacpacPath, sourceDbName), cancellationToken).ConfigureAwait(false);
            AnsiConsole.MarkupLine($"[green]Export complete:[/] {Markup.Escape(bacpacPath)}");

            AnsiConsole.MarkupLine($"[yellow]Dropping and recreating target database[/] [bold]{Markup.Escape(targetDbName)}[/]...");
            await DropAndRecreateDatabaseAsync(targetConnectionString, targetDbName!, cancellationToken).ConfigureAwait(false);
            AnsiConsole.MarkupLine("[green]Target database reset.[/]");

            AnsiConsole.MarkupLine($"[yellow]Importing bacpac into[/] [bold]{Markup.Escape(targetDbName)}[/]...");
            var importService = new DacServices(targetConnectionString);
            importService.ProgressChanged += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Message))
                {
                    AnsiConsole.MarkupLine($"[grey]{Markup.Escape(args.Message)}[/]");
                }
            };

            await using var bacpacStream = File.OpenRead(bacpacPath);
            var bacpac = BacPackage.Load(bacpacStream);
            await Task.Run(() => importService.ImportBacpac(bacpac, targetDbName!), cancellationToken).ConfigureAwait(false);
            AnsiConsole.MarkupLine("[green]Import complete.[/]");
        }

        private static async Task<string?> GetDatabaseNameAsync(string sqlConnectionString, CancellationToken cancellationToken)
        {
            await using var sqlConnection = new SqlConnection(sqlConnectionString);
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var dbNameCmd = sqlConnection.CreateCommand();
            dbNameCmd.CommandType = CommandType.Text;
            dbNameCmd.CommandTimeout = 30;
            dbNameCmd.CommandText = "SELECT DB_NAME();";
            return (await dbNameCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))?.ToString();
        }

        private static async Task DropAndRecreateDatabaseAsync(
            string targetConnectionString,
            string targetDatabaseName,
            CancellationToken cancellationToken)
        {
            var builder = new SqlConnectionStringBuilder(targetConnectionString);
            builder.InitialCatalog = "master";

            await using var masterConnection = new SqlConnection(builder.ConnectionString);
            await masterConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var escapedDbName = EscapeSqlIdentifier(targetDatabaseName);
            var dropAndCreateSql = $@"
IF DB_ID(N'{targetDatabaseName.Replace("'", "''", StringComparison.Ordinal)}') IS NOT NULL
BEGIN
    ALTER DATABASE {escapedDbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE {escapedDbName};
END;

CREATE DATABASE {escapedDbName};";

            await using var dropAndCreateCmd = masterConnection.CreateCommand();
            dropAndCreateCmd.CommandType = CommandType.Text;
            dropAndCreateCmd.CommandTimeout = 0;
            dropAndCreateCmd.CommandText = dropAndCreateSql;
            await dropAndCreateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static string EscapeSqlIdentifier(string identifier)
        {
            // Uses bracket quoting and escapes closing brackets per T-SQL rules.
            return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
        }

        private sealed class ProgressDataReader : IDataReader
        {
            private readonly SqlDataReader _inner;
            private readonly Action<long> _onRow;
            private long _sinceLast;

            public ProgressDataReader(SqlDataReader inner, Action<long> onRow)
            {
                _inner = inner;
                _onRow = onRow;
            }

            public bool Read()
            {
                var hasRow = _inner.Read();
                if (hasRow)
                {
                    _sinceLast++;
                    if (_sinceLast >= 1000)
                    {
                        _onRow(_sinceLast);
                        _sinceLast = 0;
                    }
                }
                else if (_sinceLast > 0)
                {
                    _onRow(_sinceLast);
                    _sinceLast = 0;
                }

                return hasRow;
            }

            public int FieldCount => _inner.FieldCount;
            public object this[int i] => _inner[i];
            public object this[string name] => _inner[name];
            public void Close() => _inner.Close();
            public DataTable? GetSchemaTable() => _inner.GetSchemaTable();
            public bool NextResult() => _inner.NextResult();
            public int Depth => _inner.Depth;
            public bool IsClosed => _inner.IsClosed;
            public int RecordsAffected => _inner.RecordsAffected;
            public void Dispose() => _inner.Dispose();
            public bool GetBoolean(int i) => _inner.GetBoolean(i);
            public byte GetByte(int i) => _inner.GetByte(i);
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => _inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
            public char GetChar(int i) => _inner.GetChar(i);
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => _inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
            public IDataReader GetData(int i) => _inner.GetData(i);
            public string GetDataTypeName(int i) => _inner.GetDataTypeName(i);
            public DateTime GetDateTime(int i) => _inner.GetDateTime(i);
            public decimal GetDecimal(int i) => _inner.GetDecimal(i);
            public double GetDouble(int i) => _inner.GetDouble(i);
            public Type GetFieldType(int i) => _inner.GetFieldType(i);
            public float GetFloat(int i) => _inner.GetFloat(i);
            public Guid GetGuid(int i) => _inner.GetGuid(i);
            public short GetInt16(int i) => _inner.GetInt16(i);
            public int GetInt32(int i) => _inner.GetInt32(i);
            public long GetInt64(int i) => _inner.GetInt64(i);
            public string GetName(int i) => _inner.GetName(i);
            public int GetOrdinal(string name) => _inner.GetOrdinal(name);
            public string GetString(int i) => _inner.GetString(i);
            public object GetValue(int i) => _inner.GetValue(i);
            public int GetValues(object[] values) => _inner.GetValues(values);
            public bool IsDBNull(int i) => _inner.IsDBNull(i);
        }
    }
}