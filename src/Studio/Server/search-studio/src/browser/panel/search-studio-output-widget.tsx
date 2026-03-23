import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { CommandRegistry } from '@theia/core/lib/common';
import { SearchStudioOutputService } from '../common/search-studio-output-service';
import { SearchStudioClearOutputCommand, SearchStudioOutputWidgetIconClass, SearchStudioOutputWidgetId, SearchStudioOutputWidgetLabel } from '../search-studio-constants';

@injectable()
export class SearchStudioOutputWidget extends ReactWidget {

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    @inject(CommandRegistry)
    protected readonly _commandRegistry!: CommandRegistry;

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
        this.toDispose.push(this._outputService.onDidChangeEntries(() => this.update()));
        this.update();
    }

    protected render(): React.ReactNode {
        const entries = this._outputService.entries;

        return (
            <div style={{ padding: '12px', display: 'grid', gap: '12px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '12px' }}>
                    <div>
                        <strong>{SearchStudioOutputWidgetLabel}</strong>
                        <div style={{ marginTop: '4px', color: 'var(--theia-descriptionForeground)' }}>
                            Placeholder shell diagnostics appear here while the Studio workbench skeleton is being reviewed.
                        </div>
                    </div>
                    <button
                        type="button"
                        className="theia-button"
                        onClick={() => this._commandRegistry.executeCommand(SearchStudioClearOutputCommand.id)}
                    >
                        {SearchStudioClearOutputCommand.label}
                    </button>
                </div>
                {entries.length === 0 ? (
                    <div style={{ color: 'var(--theia-descriptionForeground)' }}>No output has been captured yet.</div>
                ) : (
                    <div style={{ display: 'grid', gap: '8px' }}>
                        {entries.map(entry => (
                            <article
                                key={entry.id}
                                style={{
                                    padding: '10px 12px',
                                    borderRadius: '8px',
                                    border: `1px solid ${entry.level === 'error'
                                        ? 'var(--theia-errorForeground)'
                                        : 'var(--theia-panel-border)'}`
                                }}
                            >
                                <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', fontSize: '0.85rem' }}>
                                    <strong>{entry.level.toUpperCase()}</strong>
                                    <span>{entry.source}</span>
                                    <span>{entry.timestamp}</span>
                                </div>
                                <div style={{ marginTop: '6px' }}>{entry.message}</div>
                            </article>
                        ))}
                    </div>
                )}
            </div>
        );
    }
}
