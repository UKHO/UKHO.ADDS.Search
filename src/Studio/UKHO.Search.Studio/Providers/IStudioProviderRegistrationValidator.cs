namespace UKHO.Search.Studio.Providers
{
    /// <summary>
    /// Validates that Studio provider registrations align with provider metadata registrations.
    /// </summary>
    public interface IStudioProviderRegistrationValidator
    {
        /// <summary>
        /// Validates the current Studio provider registrations and throws when required metadata is missing.
        /// </summary>
        void Validate();
    }
}
