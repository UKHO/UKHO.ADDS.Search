namespace UKHO.Workbench.Output
{
    /// <summary>
    /// Represents the supported severity and intent levels for shell-owned Workbench output entries.
    /// </summary>
    public enum OutputLevel
    {
        /// <summary>
        /// Indicates verbose developer-oriented diagnostics that help explain shell activity.
        /// </summary>
        Debug,

        /// <summary>
        /// Indicates normal informational output intended to explain successful shell activity.
        /// </summary>
        Info,

        /// <summary>
        /// Indicates a recoverable condition that should be visible to the user and to developers.
        /// </summary>
        Warning,

        /// <summary>
        /// Indicates a failure or serious problem that prevented an expected shell action from completing.
        /// </summary>
        Error
    }
}
