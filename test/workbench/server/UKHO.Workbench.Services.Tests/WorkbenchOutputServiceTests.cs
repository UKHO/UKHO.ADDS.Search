using Shouldly;
using UKHO.Workbench.Output;
using UKHO.Workbench.Services.Output;
using Xunit;

namespace UKHO.Workbench.Services.Tests
{
    /// <summary>
    /// Verifies the bounded in-memory Workbench output service introduced for the output foundation slice.
    /// </summary>
    public class WorkbenchOutputServiceTests
    {
        /// <summary>
        /// Confirms the service appends entries in chronological order and raises change notifications for each append.
        /// </summary>
        [Fact]
        public void AppendEntriesInChronologicalOrderAndRaiseChangeNotifications()
        {
            // The first output slice depends on a stable append-only stream, so entries must stay ordered exactly as callers publish them.
            var service = new WorkbenchOutputService();
            var notificationCount = 0;
            service.EntriesChanged += (_, _) => notificationCount++;

            service.Write(OutputLevel.Info, "Shell", "Workbench shell bootstrap completed.");
            service.Write(OutputLevel.Warning, "Module loader", "A configured module could not be loaded.");

            service.Entries.Select(entry => entry.Summary).ShouldBe([
                "Workbench shell bootstrap completed.",
                "A configured module could not be loaded."
            ]);
            notificationCount.ShouldBe(2);
        }

        /// <summary>
        /// Confirms previously read entry snapshots remain unchanged after the service later receives additional entries.
        /// </summary>
        [Fact]
        public void ReturnSnapshotCopiesSoPreviouslyReadCollectionsRemainImmutable()
        {
            // Callers should receive snapshot copies rather than a live mutable list so the retained stream cannot be modified accidentally.
            var service = new WorkbenchOutputService();

            service.Write(OutputLevel.Info, "Shell", "First entry");
            var firstSnapshot = service.Entries;

            service.Write(OutputLevel.Info, "Shell", "Second entry");

            firstSnapshot.Count.ShouldBe(1);
            firstSnapshot[0].Summary.ShouldBe("First entry");
            service.Entries.Count.ShouldBe(2);
        }

        /// <summary>
        /// Confirms the service retains only the newest entries once the configured retention boundary has been exceeded.
        /// </summary>
        [Fact]
        public void EvictTheOldestEntriesWhenTheRetentionBoundaryIsExceeded()
        {
            // Long-running sessions should keep the newest diagnostic context only, so the service removes older entries first when the limit is exceeded.
            var service = new WorkbenchOutputService();

            foreach (var index in Enumerable.Range(1, 251))
            {
                service.Write(OutputLevel.Debug, "Shell", $"Entry {index}");
            }

            service.Entries.Count.ShouldBe(250);
            service.Entries[0].Summary.ShouldBe("Entry 2");
            service.Entries[^1].Summary.ShouldBe("Entry 251");
        }

        /// <summary>
        /// Confirms hidden-panel output tracks the most severe unseen level and clears that indicator when the panel is opened.
        /// </summary>
        [Fact]
        public void TrackTheMostSevereHiddenUnseenLevelAndClearItWhenThePanelOpens()
        {
            // The collapsed-panel indicator should summarize unseen severity so users can judge urgency without auto-opening the panel.
            var service = new WorkbenchOutputService();

            service.Write(OutputLevel.Info, "Shell", "Informational startup output.");
            service.Write(OutputLevel.Error, "Shell", "A startup failure occurred.");

            service.PanelState.HiddenUnseenLevel.ShouldBe(OutputLevel.Error);

            service.SetPanelVisibility(true);

            service.PanelState.HiddenUnseenLevel.ShouldBeNull();
        }

        /// <summary>
        /// Confirms a new session defaults the output pane to showing informational output and above.
        /// </summary>
        [Fact]
        public void InitializeTheMinimumVisibleOutputLevelToInfoAndAboveByDefault()
        {
            // The default session view should be quieter than the raw retained stream, so new sessions start at the Info threshold.
            var service = new WorkbenchOutputService();

            service.PanelState.MinimumVisibleLevel.ShouldBe(OutputLevel.Info);
        }

        /// <summary>
        /// Confirms the shared panel state tracks the current minimum visible output level for the active session.
        /// </summary>
        [Fact]
        public void TrackTheCurrentMinimumVisibleOutputLevelInTheSharedPanelState()
        {
            // The output-pane selector needs one session-scoped source of truth so the toolbar and visible output stay aligned.
            var service = new WorkbenchOutputService();

            service.SetMinimumVisibleLevel(OutputLevel.Error);

            service.PanelState.MinimumVisibleLevel.ShouldBe(OutputLevel.Error);
        }

        /// <summary>
        /// Confirms hidden output below the current visible threshold does not contribute to the collapsed unseen indicator.
        /// </summary>
        [Fact]
        public void IgnoreHiddenEntriesBelowTheCurrentMinimumVisibleLevelWhenTrackingTheCollapsedIndicator()
        {
            // Lower-level hidden entries should not light the collapsed indicator when the current session is intentionally filtered to higher-severity output.
            var service = new WorkbenchOutputService();
            service.SetMinimumVisibleLevel(OutputLevel.Error);

            service.Write(OutputLevel.Warning, "Shell", "A warning was written while the panel was collapsed.");

            service.PanelState.HiddenUnseenLevel.ShouldBeNull();

            service.Write(OutputLevel.Error, "Shell", "An error was written while the panel was collapsed.");

            service.PanelState.HiddenUnseenLevel.ShouldBe(OutputLevel.Error);
        }

        /// <summary>
        /// Confirms clearing the stream removes all retained entries, resets the hidden severity indicator, and avoids synthetic replacement output.
        /// </summary>
        [Fact]
        public void ClearTheStreamWithoutAddingSyntheticReplacementEntries()
        {
            // Clearing should leave the output surface genuinely empty because the specification explicitly rejects a replacement "Output cleared" message.
            var service = new WorkbenchOutputService();

            service.Write(OutputLevel.Warning, "Shell", "A hidden warning is waiting.");

            service.Clear();

            service.Entries.ShouldBeEmpty();
            service.PanelState.HiddenUnseenLevel.ShouldBeNull();
        }

        /// <summary>
        /// Confirms session-only pane heights survive close and reopen while expanded-row state is reset.
        /// </summary>
        [Fact]
        public void PreserveUserAdjustedHeightsAcrossCloseAndReopenWhileResettingExpandedRows()
        {
            // Reopening the panel in the same session should restore the last splitter size but start with all rows collapsed again.
            var service = new WorkbenchOutputService();

            service.SetPanelVisibility(true);
            service.SetPaneHeights("640px", "160px");
            service.SetExpandedEntryIds(["entry-1", "entry-2"]);

            service.SetPanelVisibility(false);
            service.SetPanelVisibility(true);

            service.PanelState.IsVisible.ShouldBeTrue();
            service.PanelState.CenterPaneHeight.ShouldBe("640px");
            service.PanelState.OutputPaneHeight.ShouldBe("160px");
            service.PanelState.ExpandedEntryIds.ShouldBeEmpty();
        }

        /// <summary>
        /// Confirms the shared panel state tracks wrap and auto-scroll flags for the current session.
        /// </summary>
        [Fact]
        public void TrackWrapAndAutoScrollFlagsInTheSharedPanelState()
        {
            // Toolbar state must live with the shared panel session model so the shell can re-render accurately after interactions.
            var service = new WorkbenchOutputService();

            service.SetWordWrapEnabled(true);
            service.SetAutoScrollEnabled(false);

            service.PanelState.IsWordWrapEnabled.ShouldBeTrue();
            service.PanelState.IsAutoScrollEnabled.ShouldBeFalse();
        }
    }
}
