using System.Globalization;
using MaxRev.Gdal.Core;
using Microsoft.Extensions.Logging;
using OSGeo.GDAL;
using OSGeo.OGR;
using UKHO.Search.Ingestion.Pipeline.Documents;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers.Enrichers
{
    internal sealed class BasicS57Enricher : IS57Enricher
    {
        private static readonly object ConfigureLock = new();
        private static bool _configured;

        private readonly ILogger<BasicS57Enricher> _logger;

        public BasicS57Enricher(ILogger<BasicS57Enricher> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public bool TryParse(string pathTo000, CanonicalDocument document)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pathTo000);
            ArgumentNullException.ThrowIfNull(document);

            TryConfigureGdal();

            try
            {
                using var ds = Ogr.Open(pathTo000, 0);
                if (ds is null)
                {
                    _logger.LogWarning("Failed to open S-57 dataset. FilePath={FilePath}", pathTo000);
                    return false;
                }

                var envelope = GetDatasetEnvelope(ds);
                var coveragePolygonWkt = BuildEnvelopePolygonWkt(envelope);
                _logger.LogDebug("S-57 envelope polygon computed. FilePath={FilePath} PolygonWkt={PolygonWkt}", pathTo000, coveragePolygonWkt);

                // Text extraction: DSID.DSID_COMT and DSID.DSPM_COMT (in sample both are "Produced by NOAA").
                var textValues = ExtractDsidComments(ds);
                foreach (var text in textValues)
                {
                    _logger.LogDebug("S-57 text metadata extracted. FilePath={FilePath} Value={Value}", pathTo000, text);
                    document.AddSearchText(text);
                }

                try
                {
                    var polygon = GeoPolygonWktReader.ReadPolygon(coveragePolygonWkt);
                    document.AddGeoPolygon(polygon);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse S57 coverage polygon WKT. FilePath={FilePath}", pathTo000);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse S-57 dataset. FilePath={FilePath}", pathTo000);
                return false;
            }
        }

        private static void TryConfigureGdal()
        {
            if (_configured)
            {
                return;
            }

            lock (ConfigureLock)
            {
                if (_configured)
                {
                    return;
                }

                GdalBase.ConfigureAll();
                Gdal.UseExceptions();
                Ogr.UseExceptions();

                _configured = true;
            }
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

        private static string BuildEnvelopePolygonWkt(Envelope env)
        {
            // POLYGON((minX minY, minX maxY, maxX maxY, maxX minY, minX minY))
            var minX = env.MinX.ToString("R", CultureInfo.InvariantCulture);
            var minY = env.MinY.ToString("R", CultureInfo.InvariantCulture);
            var maxX = env.MaxX.ToString("R", CultureInfo.InvariantCulture);
            var maxY = env.MaxY.ToString("R", CultureInfo.InvariantCulture);
            return $"POLYGON(({minX} {minY}, {minX} {maxY}, {maxX} {maxY}, {maxX} {minY}, {minX} {minY}))";
        }

        private static IReadOnlyList<string> ExtractDsidComments(DataSource ds)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < ds.GetLayerCount(); i++)
            {
                using var layer = ds.GetLayerByIndex(i);
                if (layer is null)
                {
                    continue;
                }

                var layerName = layer.GetName();
                if (!string.Equals(layerName, "Meta", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(layerName, "DSID", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var defn = layer.GetLayerDefn();
                if (defn is null)
                {
                    continue;
                }

                var dsidComtIndex = FindFieldIndex(defn, "DSID_COMT");
                var dspmComtIndex = FindFieldIndex(defn, "DSPM_COMT");
                if (dsidComtIndex < 0 && dspmComtIndex < 0)
                {
                    continue;
                }

                layer.ResetReading();

                Feature? feat;
                while ((feat = layer.GetNextFeature()) is not null)
                {
                    using (feat)
                    {
                        TryAdd(values, feat, dsidComtIndex);
                        TryAdd(values, feat, dspmComtIndex);
                    }
                }
            }

            return values
                .Select(v => v.Trim().ToLowerInvariant())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();
        }

        private static int FindFieldIndex(FeatureDefn defn, string name)
        {
            for (var i = 0; i < defn.GetFieldCount(); i++)
            {
                using var fd = defn.GetFieldDefn(i);
                if (fd is null)
                {
                    continue;
                }

                if (string.Equals(fd.GetName(), name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void TryAdd(HashSet<string> values, Feature feat, int index)
        {
            if (index < 0)
            {
                return;
            }

            try
            {
                var val = feat.GetFieldAsString(index);
                if (!string.IsNullOrWhiteSpace(val))
                {
                    values.Add(val);
                }
            }
            catch
            {
                // Best-effort.
            }
        }
    }
}
