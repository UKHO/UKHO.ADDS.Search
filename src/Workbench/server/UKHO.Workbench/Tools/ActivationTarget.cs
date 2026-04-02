using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Tools
{
    /// <summary>
    /// Describes the shell target used when a tool activation request is issued.
    /// </summary>
    public class ActivationTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivationTarget"/> class.
        /// </summary>
        /// <param name="toolId">The identifier of the tool that should be opened or focused.</param>
        /// <param name="region">The shell region that should host the tool.</param>
        /// <param name="logicalTabKey">The bounded logical key that determines whether an existing open tab should be reused.</param>
        /// <param name="parameterIdentity">The bounded parameter identity that distinguishes one logical target of the same tool from another.</param>
        /// <param name="initialTitle">The initial tab title that should be shown until hosted-view metadata supersedes it.</param>
        /// <param name="initialIcon">The initial tab icon that should be shown until hosted-view metadata supersedes it.</param>
        public ActivationTarget(
            string toolId,
            WorkbenchShellRegion region,
            string? logicalTabKey = null,
            string? parameterIdentity = null,
            string? initialTitle = null,
            string? initialIcon = null)
        {
            // Activation requests carry bounded identity and metadata so explorer, command, and tool-context entry points all flow through the same tab-reuse contract.
            ArgumentException.ThrowIfNullOrWhiteSpace(toolId);

            ToolId = toolId;
            Region = region;
            ParameterIdentity = NormalizeOptionalValue(parameterIdentity);
            LogicalTabKey = ResolveLogicalTabKey(toolId, logicalTabKey, ParameterIdentity);
            InitialTitle = NormalizeOptionalValue(initialTitle);
            InitialIcon = NormalizeOptionalValue(initialIcon);
        }

        /// <summary>
        /// Gets the identifier of the tool that should be opened or focused.
        /// </summary>
        public string ToolId { get; }

        /// <summary>
        /// Gets the shell region that should host the tool.
        /// </summary>
        public WorkbenchShellRegion Region { get; }

        /// <summary>
        /// Gets the bounded logical key that determines whether an existing open tab should be reused.
        /// </summary>
        public string LogicalTabKey { get; }

        /// <summary>
        /// Gets the bounded parameter identity that distinguishes one logical target of the same tool from another.
        /// </summary>
        public string? ParameterIdentity { get; }

        /// <summary>
        /// Gets the initial tab title that should be shown until hosted-view metadata supersedes it.
        /// </summary>
        public string? InitialTitle { get; }

        /// <summary>
        /// Gets the initial tab icon that should be shown until hosted-view metadata supersedes it.
        /// </summary>
        public string? InitialIcon { get; }

        /// <summary>
        /// Creates the logical Workbench tab identity represented by this activation request.
        /// </summary>
        /// <returns>The logical tab identity used by the shell for open-or-focus decisions.</returns>
        public WorkbenchTabIdentity CreateTabIdentity()
        {
            // Tab identity creation is centralized here so explorer, command, and tool-context activation paths all resolve reuse consistently.
            return new WorkbenchTabIdentity(ToolId, Region, LogicalTabKey, ParameterIdentity);
        }

        /// <summary>
        /// Creates an activation target that hosts the tool inside the central tool surface.
        /// </summary>
        /// <param name="toolId">The identifier of the tool that should be opened or focused.</param>
        /// <param name="logicalTabKey">The bounded logical key that determines whether an existing open tab should be reused.</param>
        /// <param name="parameterIdentity">The bounded parameter identity that distinguishes one logical target of the same tool from another.</param>
        /// <param name="initialTitle">The initial tab title that should be shown until hosted-view metadata supersedes it.</param>
        /// <param name="initialIcon">The initial tab icon that should be shown until hosted-view metadata supersedes it.</param>
        /// <returns>A tool-surface activation target for the supplied tool identifier.</returns>
        public static ActivationTarget CreateToolSurfaceTarget(
            string toolId,
            string? logicalTabKey = null,
            string? parameterIdentity = null,
            string? initialTitle = null,
            string? initialIcon = null)
        {
            // The bootstrap slice only supports tool hosting in the center working surface.
            return new ActivationTarget(
                toolId,
                WorkbenchShellRegion.ToolSurface,
                logicalTabKey,
                parameterIdentity,
                initialTitle,
                initialIcon);
        }

        /// <summary>
        /// Normalizes an optional string value so whitespace-only values are treated as absent.
        /// </summary>
        /// <param name="value">The optional value that should be normalized.</param>
        /// <returns>The normalized optional value, or <see langword="null"/> when no meaningful value was supplied.</returns>
        private static string? NormalizeOptionalValue(string? value)
        {
            // Activation metadata should not preserve whitespace-only values because the shell treats missing metadata and empty metadata the same way.
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Resolves the logical tab key that should be used for tab reuse decisions.
        /// </summary>
        /// <param name="toolId">The identifier of the tool being activated.</param>
        /// <param name="logicalTabKey">The explicit logical tab key supplied by the caller, when one exists.</param>
        /// <param name="parameterIdentity">The bounded parameter identity supplied by the caller, when one exists.</param>
        /// <returns>The logical tab key that should be used for tab reuse decisions.</returns>
        private static string ResolveLogicalTabKey(string toolId, string? logicalTabKey, string? parameterIdentity)
        {
            // Explicit logical keys still win so callers can opt into custom identity shapes, but parameter identity now provides the default differentiation for same-tool requests.
            if (!string.IsNullOrWhiteSpace(logicalTabKey))
            {
                return logicalTabKey;
            }

            return string.IsNullOrWhiteSpace(parameterIdentity)
                ? toolId
                : $"{toolId}::{parameterIdentity}";
        }
    }
}
