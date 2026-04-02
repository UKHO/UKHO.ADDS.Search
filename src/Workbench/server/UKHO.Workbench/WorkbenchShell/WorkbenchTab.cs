using UKHO.Workbench.Tools;

namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Represents one open Workbench tab together with the runtime tool instance hosted behind that tab.
    /// </summary>
    public class WorkbenchTab
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchTab"/> class.
        /// </summary>
        /// <param name="identity">The logical identity that determines whether future activation requests reuse this tab.</param>
        /// <param name="toolInstance">The runtime tool instance hosted by the tab.</param>
        /// <param name="openedAtUtc">The UTC timestamp recorded when the tab was first opened.</param>
        public WorkbenchTab(WorkbenchTabIdentity identity, ToolInstance toolInstance, DateTimeOffset openedAtUtc)
        {
            // Tabs keep both logical identity and runtime instance state so the shell can manage reuse without discarding hosted component state.
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            ToolInstance = toolInstance ?? throw new ArgumentNullException(nameof(toolInstance));
            OpenedAtUtc = openedAtUtc;
            LastActivatedAtUtc = openedAtUtc;
        }

        /// <summary>
        /// Gets the stable tab identifier used by the shell for switching and close operations.
        /// </summary>
        public string Id => ToolInstance.InstanceId;

        /// <summary>
        /// Gets the logical identity that determines whether future activation requests reuse this tab.
        /// </summary>
        public WorkbenchTabIdentity Identity { get; }

        /// <summary>
        /// Gets the bounded parameter identity that distinguishes one logical target of the same tool from another.
        /// </summary>
        public string? ParameterIdentity => Identity.ParameterIdentity;

        /// <summary>
        /// Gets the runtime tool instance hosted by the tab.
        /// </summary>
        public ToolInstance ToolInstance { get; }

        /// <summary>
        /// Gets the title currently shown for the tab.
        /// </summary>
        public string Title => ToolInstance.Title;

        /// <summary>
        /// Gets the icon currently shown for the tab.
        /// </summary>
        public string Icon => ToolInstance.Icon;

        /// <summary>
        /// Gets a value indicating whether the tab can be closed.
        /// </summary>
        public bool IsClosable => true;

        /// <summary>
        /// Gets the UTC timestamp recorded when the tab was first opened.
        /// </summary>
        public DateTimeOffset OpenedAtUtc { get; }

        /// <summary>
        /// Gets the UTC timestamp recorded when the tab most recently became active.
        /// </summary>
        public DateTimeOffset LastActivatedAtUtc { get; private set; }

        /// <summary>
        /// Marks the tab as the currently active tab.
        /// </summary>
        /// <param name="activatedAtUtc">The UTC timestamp that should be recorded for the activation.</param>
        public void MarkActivated(DateTimeOffset activatedAtUtc)
        {
            // Activation timestamps support most-recently-active close behavior without reordering the visible tab strip itself.
            LastActivatedAtUtc = activatedAtUtc;
            ToolInstance.MarkActivated(activatedAtUtc);
        }
    }
}
