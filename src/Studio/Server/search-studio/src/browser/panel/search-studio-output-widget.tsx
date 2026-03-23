import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { SearchStudioOutputService } from '../common/search-studio-output-service';
import { SearchStudioOutputWidgetIconClass, SearchStudioOutputWidgetId, SearchStudioOutputWidgetLabel } from '../search-studio-constants';
import { formatOutputEntryText, formatOutputSeverity, formatOutputTimestamp, getOutputSeverityColor, getRevealLatestScrollPosition } from './search-studio-output-format';

@injectable()
export class SearchStudioOutputWidget extends ReactWidget {

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    protected _outputContainer: HTMLDivElement | undefined;
    protected _hasPendingRevealLatest = false;

    protected readonly _captureOutputContainer = (element: HTMLDivElement | null): void => {
        this._outputContainer = element ?? undefined;
        this.revealLatestIfNeeded();
    };

    constructor() {
        super();
        this.id = SearchStudioOutputWidgetId;
        this.title.label = SearchStudioOutputWidgetLabel;
        this.title.caption = 'Studio shell output and diagnostics';
        this.title.iconClass = SearchStudioOutputWidgetIconClass;
        this.title.closable = true;
        this.addClass('search-studio-output-widget');
    }

    @postConstruct()
    protected init(): void {
        this.toDispose.push(this._outputService.onDidChangeEntries(() => {
            this._hasPendingRevealLatest = true;
            this.update();
            this.scheduleRevealLatest();
        }));
        this.update();
    }

    protected scheduleRevealLatest(): void {
        if (typeof window !== 'undefined' && typeof window.requestAnimationFrame === 'function') {
            window.requestAnimationFrame(() => this.revealLatestIfNeeded());
            return;
        }

        setTimeout(() => this.revealLatestIfNeeded(), 0);
    }

    protected revealLatestIfNeeded(): void {
        if (!this._hasPendingRevealLatest || !this._outputContainer) {
            return;
        }

        this._outputContainer.scrollTop = getRevealLatestScrollPosition(
            this._outputService.entries.length,
            this._outputContainer.scrollHeight);
        this._hasPendingRevealLatest = false;
    }

    protected render(): React.ReactNode {
        const entries = this._outputService.entries;
        const fontFamily = 'var(--theia-editor-font-family, var(--theia-ui-font-family))';
        const tokenColor = 'var(--theia-descriptionForeground)';

        return (
            <div
                role="log"
                aria-label={SearchStudioOutputWidgetLabel}
                ref={this._captureOutputContainer}
                style={{
                    padding: '6px 8px',
                    display: 'flex',
                    flexDirection: 'column',
                    fontFamily,
                    fontSize: '0.8rem',
                    lineHeight: 1.4,
                    fontVariantNumeric: 'tabular-nums',
                    boxSizing: 'border-box',
                    height: '100%',
                    overflowY: 'auto',
                    overflowX: 'hidden'
                }}
            >
                {entries.length === 0 ? (
                    <div style={{ color: 'var(--theia-descriptionForeground)', padding: '2px 0' }}>
                        No output has been captured yet.
                    </div>
                ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'stretch', gap: '0' }}>
                        {entries.map(entry => {
                            const lineText = formatOutputEntryText(entry);

                            return (
                                <div
                                    key={entry.id}
                                    aria-label={lineText}
                                    title={lineText}
                                    style={{
                                        display: 'flex',
                                        alignItems: 'baseline',
                                        gap: '0.625rem',
                                        padding: '1px 0',
                                        minWidth: 0
                                    }}
                                >
                                    <span style={{ color: tokenColor, whiteSpace: 'nowrap', flex: '0 0 auto' }}>
                                        {formatOutputTimestamp(entry.timestamp)}
                                    </span>
                                    <strong
                                        style={{
                                            color: getOutputSeverityColor(entry.level),
                                            whiteSpace: 'nowrap',
                                            flex: '0 0 auto',
                                            fontWeight: 600,
                                            letterSpacing: '0.04em'
                                        }}
                                    >
                                        {formatOutputSeverity(entry.level)}
                                    </strong>
                                    <span style={{ color: tokenColor, whiteSpace: 'nowrap', flex: '0 0 auto' }}>
                                        {entry.source}
                                    </span>
                                    <span style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere', minWidth: 0, flex: '1 1 auto' }}>
                                        {entry.message}
                                    </span>
                                </div>
                            );
                        })}
                    </div>
                )}
            </div>
        );
    }
}
