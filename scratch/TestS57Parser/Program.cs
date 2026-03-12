using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.Globalization;
using System.Text;

namespace TestS57Parser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GdalBase.ConfigureAll();

            Gdal.UseExceptions();
            Ogr.UseExceptions();

            var inputPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "sample.000");

            Console.WriteLine($"Input: {inputPath}");
            Console.WriteLine($"Exists: {File.Exists(inputPath)}");
            Console.WriteLine();

            Console.WriteLine($"GDAL Version: {Gdal.VersionInfo("RELEASE_NAME")}");
            Console.WriteLine();

            Console.WriteLine("Drivers:");
            Console.WriteLine($"  OGR: {Ogr.GetDriverCount()} drivers registered");
            Console.WriteLine($"  GDAL: {Gdal.GetDriverCount()} drivers registered");
            Console.WriteLine();

            DataSource? ds = null;
            try
            {
                ds = Ogr.Open(inputPath, 0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("OGR.Open failed.");
                Console.Error.WriteLine(ex);
                Environment.ExitCode = 1;
                return;
            }

            if (ds is null)
            {
                Console.Error.WriteLine("Failed to open dataset (null). Ensure S-57 driver is available and file path is correct.");
                Environment.ExitCode = 1;
                return;
            }

            using (ds)
            {
                PrintTextualInfo(ds);
                var env = GetDatasetEnvelope(ds);
                Console.WriteLine($"Coverage boundary (envelope polygon): {BuildEnvelopePolygonWkt(env)}");
            }
        }

        private static void PrintTextualInfo(DataSource ds)
        {
            Console.WriteLine($"Name: {ds.name}");
            var driver = ds.GetDriver();
            if (driver is not null)
            {
                Console.WriteLine($"Driver: {driver.GetName()}");
            }

            Console.WriteLine($"GDAL: {Gdal.VersionInfo("RELEASE_NAME")}");
            Console.WriteLine();

            // Attempt to extract human-friendly text from the Meta layer and any DSID/DSNM-like attributes.
            Console.WriteLine("Textual info (best-effort):");
            var text = ExtractTextualInfo(ds, maxFeaturesToScan: 2000);
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("  <none found>");
            }
            else
            {
                Console.WriteLine(text);
            }

            Console.WriteLine();
        }

        private static Envelope GetDatasetEnvelope(DataSource ds)
        {
            var env = new Envelope();
            var has = false;

            for (var i = 0; i < ds.GetLayerCount(); i++)
            {
                using var layer = ds.GetLayerByIndex(i);
                if (layer is null)
                {
                    continue;
                }

                var layerEnv = new Envelope();
                try
                {
                    if (layer.GetExtent(layerEnv, 1) != 0)
                    {
                        continue;
                    }
                }
                catch
                {
                    continue;
                }

                if (!has)
                {
                    env = layerEnv;
                    has = true;
                }
                else
                {
                    env.MinX = Math.Min(env.MinX, layerEnv.MinX);
                    env.MinY = Math.Min(env.MinY, layerEnv.MinY);
                    env.MaxX = Math.Max(env.MaxX, layerEnv.MaxX);
                    env.MaxY = Math.Max(env.MaxY, layerEnv.MaxY);
                }
            }

            return env;
        }

        private static string ExtractTextualInfo(DataSource ds, int maxFeaturesToScan)
        {
            var sb = new StringBuilder();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < ds.GetLayerCount(); i++)
            {
                using var layer = ds.GetLayerByIndex(i);
                if (layer is null)
                {
                    continue;
                }

                // Meta is the likeliest place, but scan all layers - a capped scan keeps it cheap.
                layer.ResetReading();
                var layerDef = layer.GetLayerDefn();
                if (layerDef is null)
                {
                    continue;
                }

                var fieldCount = layerDef.GetFieldCount();
                if (fieldCount <= 0)
                {
                    continue;
                }

                var scanned = 0;
                Feature? feat;
                while (scanned < maxFeaturesToScan && (feat = layer.GetNextFeature()) is not null)
                {
                    using (feat)
                    {
                        scanned++;

                        // Prefer string-like fields commonly used for identification.
                        for (var f = 0; f < fieldCount; f++)
                        {
                            using var defn = layerDef.GetFieldDefn(f);
                            if (defn is null)
                            {
                                continue;
                            }

                            var fieldType = defn.GetFieldType();
                            if (fieldType != FieldType.OFTString && fieldType != FieldType.OFTStringList)
                            {
                                continue;
                            }

                            var name = defn.GetName();
                            if (!LooksLikeUsefulTextField(name))
                            {
                                continue;
                            }

                            string? val = null;
                            try
                            {
                                val = feat.GetFieldAsString(f);
                            }
                            catch
                            {
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(val))
                            {
                                continue;
                            }

                            var key = $"{layer.GetName()}::{name}::{val}";
                            if (!seen.Add(key))
                            {
                                continue;
                            }

                            sb.Append("  ");
                            sb.Append(layer.GetName());
                            sb.Append(".");
                            sb.Append(name);
                            sb.Append(" = ");
                            sb.AppendLine(val);
                        }
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static bool LooksLikeUsefulTextField(string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            // S-57 typical identifying / descriptive candidates.
            return fieldName.Equals("DSNM", StringComparison.OrdinalIgnoreCase)
                || fieldName.Equals("DSID", StringComparison.OrdinalIgnoreCase)
                || fieldName.Equals("DSPM_COMT", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_COMT", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("NAME", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("DESC", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildEnvelopePolygonWkt(Envelope env)
        {
            // POLYGON((minX minY, minX maxY, maxX maxY, maxX minY, minX minY))
            var minX = env.MinX.ToString("R", CultureInfo.InvariantCulture);
            var minY = env.MinY.ToString("R", CultureInfo.InvariantCulture);
            var maxX = env.MaxX.ToString("R", CultureInfo.InvariantCulture);
            var maxY = env.MaxY.ToString("R", CultureInfo.InvariantCulture);
            return $"POLYGON(({minX} {minY}, {minX} {maxY}, {maxX} {maxY}, {maxX} {minY}, {minX} {minY}))";
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength] + "...";
        }
    }
}
