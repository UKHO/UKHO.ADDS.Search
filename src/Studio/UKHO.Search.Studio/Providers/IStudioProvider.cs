namespace UKHO.Search.Studio.Providers
{
    /// <summary>
    /// Defines the minimal contract that every Studio provider must expose.
    /// </summary>
    public interface IStudioProvider
    {
        /// <summary>
        /// Gets the stable provider name used to resolve the provider within Studio workflows.
        /// </summary>
        string ProviderName { get; }
    }
}
