namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Describes the logical identity used by the Workbench shell to decide whether an activation request should open a new tab or focus an existing tab.
    /// </summary>
    public class WorkbenchTabIdentity : IEquatable<WorkbenchTabIdentity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchTabIdentity"/> class.
        /// </summary>
        /// <param name="toolId">The identifier of the tool that owns the logical tab.</param>
        /// <param name="region">The shell region that hosts the tab.</param>
        /// <param name="logicalTabKey">The bounded logical key used to distinguish one tab target from another.</param>
        /// <param name="parameterIdentity">The bounded parameter identity that further distinguishes one logical target of the same tool from another.</param>
        public WorkbenchTabIdentity(string toolId, WorkbenchShellRegion region, string logicalTabKey, string? parameterIdentity = null)
        {
            // The shell uses a bounded logical key so reopening the same target can reuse a tab without comparing component instances directly.
            ArgumentException.ThrowIfNullOrWhiteSpace(toolId);
            ArgumentException.ThrowIfNullOrWhiteSpace(logicalTabKey);

            ToolId = toolId;
            Region = region;
            LogicalTabKey = logicalTabKey;
            ParameterIdentity = string.IsNullOrWhiteSpace(parameterIdentity) ? null : parameterIdentity;
        }

        /// <summary>
        /// Gets the identifier of the tool that owns the logical tab.
        /// </summary>
        public string ToolId { get; }

        /// <summary>
        /// Gets the shell region that hosts the tab.
        /// </summary>
        public WorkbenchShellRegion Region { get; }

        /// <summary>
        /// Gets the bounded logical key used to distinguish one tab target from another.
        /// </summary>
        public string LogicalTabKey { get; }

        /// <summary>
        /// Gets the bounded parameter identity that further distinguishes one logical target of the same tool from another.
        /// </summary>
        public string? ParameterIdentity { get; }

        /// <summary>
        /// Determines whether the current tab identity matches another identity instance.
        /// </summary>
        /// <param name="other">The other identity instance to compare.</param>
        /// <returns><see langword="true"/> when both identities represent the same logical tab target; otherwise, <see langword="false"/>.</returns>
        public bool Equals(WorkbenchTabIdentity? other)
        {
            // Logical identity comparisons stay explicit so the shell can reuse tabs deterministically across activation paths.
            return other is not null
                && string.Equals(ToolId, other.ToolId, StringComparison.Ordinal)
                && Region == other.Region
                && string.Equals(LogicalTabKey, other.LogicalTabKey, StringComparison.Ordinal)
                && string.Equals(ParameterIdentity, other.ParameterIdentity, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the current tab identity matches the supplied object instance.
        /// </summary>
        /// <param name="obj">The object instance to compare.</param>
        /// <returns><see langword="true"/> when the supplied object is an equivalent <see cref="WorkbenchTabIdentity"/>; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            // Object-based equality delegates to the strongly typed comparison so all shell lookups follow the same rules.
            return Equals(obj as WorkbenchTabIdentity);
        }

        /// <summary>
        /// Returns the hash code for the current logical tab identity.
        /// </summary>
        /// <returns>The combined hash code for the tool identifier, region, and logical key.</returns>
        public override int GetHashCode()
        {
            // The hash code combines the complete logical identity because any differing part represents a different reusable tab target.
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(ToolId),
                Region,
                StringComparer.Ordinal.GetHashCode(LogicalTabKey),
                ParameterIdentity is null ? 0 : StringComparer.Ordinal.GetHashCode(ParameterIdentity));
        }
    }
}
