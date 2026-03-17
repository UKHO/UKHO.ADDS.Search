namespace UKHO.Aspire.Configuration
{
    public static class WellKnownConfigurationName
    {
        public const string AddsEnvironmentName = "adds-environment";

        public const string ConfigurationFilePath = "configuration-path";

        public const string ExternalServicesFilePath = "external-service-path";

        /// <summary>
        /// Optional root directory containing additional configuration files to ingest.
        /// </summary>
        public const string AdditionalConfigurationPath = "additional-configuration-path";

        /// <summary>
        /// Optional key prefix used when writing additional configuration to App Configuration.
        /// </summary>
        public const string AdditionalConfigurationPrefix = "additional-configuration-prefix";

        public const string ConfigurationServiceName = "adds-configuration";

        public const string ConfigurationSeederName = "adds-configuration-local-seeder";

        public const string ServiceName = "adds-configuration-host-service-name";

        public const string ExternalServiceKeyPrefix = "externalservice";

        public const string ReloadSentinelKey = "auto.reload.sentinel";
    }
}