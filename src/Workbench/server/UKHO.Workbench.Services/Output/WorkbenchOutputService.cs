using UKHO.Workbench.Output;

namespace UKHO.Workbench.Services.Output
{
    /// <summary>
    /// Stores a bounded in-memory Workbench output stream for the current host session.
    /// </summary>
    public sealed class WorkbenchOutputService : IWorkbenchOutputService
    {
        private const int RetentionLimit = 250;
        private const string DefaultCenterPaneHeight = "4*";
        private const string DefaultOutputPaneHeight = "1*";
        private readonly List<OutputEntry> _entries = [];
        private readonly object _entriesLock = new();
        private readonly object _panelStateLock = new();
        private OutputPanelState _panelState = CreateDefaultPanelState();

        /// <summary>
        /// Occurs when the visible output-entry collection has changed.
        /// </summary>
        public event EventHandler? EntriesChanged;

        /// <summary>
        /// Occurs when the shared output-panel session state has changed.
        /// </summary>
        public event EventHandler? PanelStateChanged;

        /// <summary>
        /// Gets the current output entries in chronological order.
        /// </summary>
        public IReadOnlyList<OutputEntry> Entries
        {
            get
            {
                // The service returns a snapshot copy so callers cannot mutate the retained session stream accidentally.
                lock (_entriesLock)
                {
                    return _entries.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the current shell-owned output-panel session state.
        /// </summary>
        public OutputPanelState PanelState
        {
            get
            {
                // The service returns the immutable panel-state snapshot so callers always read a stable view of the current session state.
                lock (_panelStateLock)
                {
                    return _panelState;
                }
            }
        }

        /// <summary>
        /// Creates and appends a new immutable output entry using the current UTC timestamp.
        /// </summary>
        /// <param name="level">The severity or intent level assigned to the new entry.</param>
        /// <param name="source">The subsystem, tool, or shell area that is emitting the entry.</param>
        /// <param name="summary">The compact summary text that should be rendered in the stream.</param>
        /// <param name="details">Optional longer diagnostic detail associated with the summary.</param>
        /// <param name="eventCode">Optional stable event code that callers can use to correlate repeated messages.</param>
        /// <returns>The immutable entry instance that was appended to the stream.</returns>
        public OutputEntry Write(
            OutputLevel level,
            string source,
            string summary,
            string? details = null,
            string? eventCode = null)
        {
            // The convenience overload keeps callers lightweight by generating a stable identifier and timestamp centrally.
            var entry = new OutputEntry(
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow,
                level,
                source,
                summary,
                details,
                eventCode);

            Write(entry);
            return entry;
        }

        /// <summary>
        /// Appends an already-created immutable output entry to the shell-wide stream.
        /// </summary>
        /// <param name="entry">The immutable entry that should be appended to the stream.</param>
        public void Write(OutputEntry entry)
        {
            // The append path keeps ordering stable by always adding to the tail before trimming the oldest retained entries.
            ArgumentNullException.ThrowIfNull(entry);
            var panelStateChanged = false;

            lock (_entriesLock)
            {
                _entries.Add(entry);
                TrimEntriesToRetentionLimit();
            }

            lock (_panelStateLock)
            {
                if (!_panelState.IsVisible)
                {
                    var unseenLevel = GetMostSevereVisibleLevel(_panelState.HiddenUnseenLevel, entry.Level, _panelState.MinimumVisibleLevel);
                    if (_panelState.HiddenUnseenLevel != unseenLevel)
                    {
                        _panelState = _panelState with
                        {
                            HiddenUnseenLevel = unseenLevel
                        };
                        panelStateChanged = true;
                    }
                }
            }

            RaiseEntriesChanged();

            if (panelStateChanged)
            {
                RaisePanelStateChanged();
            }
        }

        /// <summary>
        /// Removes every retained output entry from the current session stream.
        /// </summary>
        public void Clear()
        {
            var hadEntries = false;
            var panelStateChanged = false;

            // Clearing the stream is a bounded in-memory operation, so the implementation simply resets the retained collection.
            lock (_entriesLock)
            {
                if (_entries.Count > 0)
                {
                    _entries.Clear();
                    hadEntries = true;
                }
            }

            lock (_panelStateLock)
            {
                if (_panelState.HiddenUnseenLevel is not null || _panelState.ExpandedEntryIds.Count > 0)
                {
                    _panelState = _panelState with
                    {
                        HiddenUnseenLevel = null,
                        ExpandedEntryIds = []
                    };
                    panelStateChanged = true;
                }
            }

            if (hadEntries)
            {
                RaiseEntriesChanged();
            }

            if (panelStateChanged)
            {
                RaisePanelStateChanged();
            }
        }

        /// <summary>
        /// Updates whether the shell output panel is currently visible.
        /// </summary>
        /// <param name="isVisible"><see langword="true"/> to mark the panel visible; otherwise, <see langword="false"/>.</param>
        public void SetPanelVisibility(bool isVisible)
        {
            var panelStateChanged = false;

            // Visibility changes also govern hidden-severity reset and expanded-row reset so the panel reopens with a clean row-expansion view.
            lock (_panelStateLock)
            {
                if (_panelState.IsVisible == isVisible)
                {
                    return;
                }

                _panelState = _panelState with
                {
                    IsVisible = isVisible,
                    HiddenUnseenLevel = isVisible ? null : _panelState.HiddenUnseenLevel,
                    ExpandedEntryIds = isVisible ? [] : _panelState.ExpandedEntryIds
                };
                panelStateChanged = true;
            }

            if (panelStateChanged)
            {
                RaisePanelStateChanged();
            }
        }

        /// <summary>
        /// Updates the current-session authored pane heights used by the shell layout.
        /// </summary>
        /// <param name="centerPaneHeight">The grid-track token that should be used for the centre working area.</param>
        /// <param name="outputPaneHeight">The grid-track token that should be used for the output panel.</param>
        public void SetPaneHeights(string centerPaneHeight, string outputPaneHeight)
        {
            // The service keeps the latest splitter-derived height tokens so reopening the panel in the same session restores the user's last size.
            ArgumentException.ThrowIfNullOrWhiteSpace(centerPaneHeight);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputPaneHeight);

            UpdatePanelState(currentState =>
            {
                if (string.Equals(currentState.CenterPaneHeight, centerPaneHeight, StringComparison.Ordinal)
                    && string.Equals(currentState.OutputPaneHeight, outputPaneHeight, StringComparison.Ordinal))
                {
                    return currentState;
                }

                return currentState with
                {
                    CenterPaneHeight = centerPaneHeight,
                    OutputPaneHeight = outputPaneHeight
                };
            });
        }

        /// <summary>
        /// Updates whether the panel should automatically keep the viewport pinned to the newest output entry.
        /// </summary>
        /// <param name="isEnabled"><see langword="true"/> to enable automatic scrolling; otherwise, <see langword="false"/>.</param>
        public void SetAutoScrollEnabled(bool isEnabled)
        {
            // Auto-scroll is tracked centrally so the shell can coordinate toolbar state, new entries, and manual scrolling rules consistently.
            UpdatePanelState(currentState => currentState.IsAutoScrollEnabled == isEnabled
                ? currentState
                : currentState with
                {
                    IsAutoScrollEnabled = isEnabled
                });
        }

        /// <summary>
        /// Updates whether the panel should wrap long output lines.
        /// </summary>
        /// <param name="isEnabled"><see langword="true"/> to enable wrapping; otherwise, <see langword="false"/>.</param>
        public void SetWordWrapEnabled(bool isEnabled)
        {
            // Word-wrap state is shared so the shell can re-render both collapsed rows and later expanded details from one global toggle.
            UpdatePanelState(currentState => currentState.IsWordWrapEnabled == isEnabled
                ? currentState
                : currentState with
                {
                    IsWordWrapEnabled = isEnabled
                });
        }

        /// <summary>
        /// Updates the minimum output level that the current session should render in the output pane.
        /// </summary>
        /// <param name="minimumVisibleLevel">The minimum output level that should remain visible in the output pane.</param>
        public void SetMinimumVisibleLevel(OutputLevel minimumVisibleLevel)
        {
            // The minimum visible level is shared session state so the output pane, hidden indicator, and later toolbar refreshes all use one authoritative threshold.
            UpdatePanelState(currentState => currentState.MinimumVisibleLevel == minimumVisibleLevel
                ? currentState
                : currentState with
                {
                    MinimumVisibleLevel = minimumVisibleLevel,
                    HiddenUnseenLevel = currentState.HiddenUnseenLevel is { } hiddenUnseenLevel
                        && hiddenUnseenLevel.IsVisibleAtOrAbove(minimumVisibleLevel)
                            ? hiddenUnseenLevel
                            : null
                });
        }

        /// <summary>
        /// Replaces the current set of expanded output-entry identifiers.
        /// </summary>
        /// <param name="expandedEntryIds">The identifiers of entries whose details are currently expanded.</param>
        public void SetExpandedEntryIds(IReadOnlyList<string> expandedEntryIds)
        {
            // Expansion state remains separate from immutable output entries so later detail folding can reset or persist view state without mutating entry data.
            ArgumentNullException.ThrowIfNull(expandedEntryIds);

            var sanitizedExpandedEntryIds = expandedEntryIds.ToArray();
            if (sanitizedExpandedEntryIds.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Expanded entry identifiers must not contain null, empty, or whitespace values.", nameof(expandedEntryIds));
            }

            UpdatePanelState(currentState => currentState.ExpandedEntryIds.SequenceEqual(sanitizedExpandedEntryIds, StringComparer.Ordinal)
                ? currentState
                : currentState with
                {
                    ExpandedEntryIds = sanitizedExpandedEntryIds
                });
        }

        /// <summary>
        /// Removes the oldest retained entries when the bounded session limit has been exceeded.
        /// </summary>
        private void TrimEntriesToRetentionLimit()
        {
            // The first slice keeps only the newest retained entries so the stream remains useful during long-running sessions without unbounded growth.
            var entriesToRemove = _entries.Count - RetentionLimit;
            if (entriesToRemove <= 0)
            {
                return;
            }

            _entries.RemoveRange(0, entriesToRemove);
        }

        /// <summary>
        /// Creates the default output-panel session state used for a new shell session.
        /// </summary>
        /// <returns>The default output-panel state.</returns>
        private static OutputPanelState CreateDefaultPanelState()
        {
            // The default state keeps the panel hidden, starts with the planned 1:4 split, and enables auto-scroll without wrapping.
            return new OutputPanelState(false, DefaultCenterPaneHeight, DefaultOutputPaneHeight);
        }

        /// <summary>
        /// Returns the most severe level from the two supplied values.
        /// </summary>
        /// <param name="currentLevel">The currently tracked unseen level.</param>
        /// <param name="candidateLevel">The newly observed level to compare.</param>
        /// <returns>The most severe of the two supplied levels.</returns>
        private static OutputLevel? GetMostSevereVisibleLevel(OutputLevel? currentLevel, OutputLevel candidateLevel, OutputLevel minimumVisibleLevel)
        {
            // Hidden indicators should ignore entries below the current visible threshold so default Info filtering does not surface hidden Debug-only noise.
            if (!candidateLevel.IsVisibleAtOrAbove(minimumVisibleLevel))
            {
                return currentLevel;
            }

            // Severity ranking remains explicit so hidden-panel indicators reflect the highest urgency seen while the panel was collapsed.
            if (currentLevel is null)
            {
                return candidateLevel;
            }

            return candidateLevel.ToSeverityRank() > currentLevel.Value.ToSeverityRank()
                ? candidateLevel
                : currentLevel.Value;
        }

        /// <summary>
        /// Updates the shared panel-state snapshot and raises the corresponding change event only when the state actually changes.
        /// </summary>
        /// <param name="updatePanelState">The callback that produces the next panel-state snapshot.</param>
        private void UpdatePanelState(Func<OutputPanelState, OutputPanelState> updatePanelState)
        {
            // Centralizing panel-state updates ensures every toolbar and layout mutation follows the same immutable replacement pattern.
            ArgumentNullException.ThrowIfNull(updatePanelState);

            var panelStateChanged = false;

            lock (_panelStateLock)
            {
                var updatedPanelState = updatePanelState(_panelState);
                if (updatedPanelState == _panelState)
                {
                    return;
                }

                _panelState = updatedPanelState;
                panelStateChanged = true;
            }

            if (panelStateChanged)
            {
                RaisePanelStateChanged();
            }
        }

        /// <summary>
        /// Raises the shared change notification after the retained output stream has changed.
        /// </summary>
        private void RaiseEntriesChanged()
        {
            // Notifications are raised outside the collection lock so UI listeners cannot block or deadlock the append path.
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the shared change notification after the panel session state has changed.
        /// </summary>
        private void RaisePanelStateChanged()
        {
            // Panel-state notifications are raised outside the state lock so the shell can re-render without risking deadlock.
            PanelStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
