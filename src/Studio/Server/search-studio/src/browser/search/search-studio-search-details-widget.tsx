import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { SearchStudioSearchService } from './search-studio-search-service';
import {
    SearchStudioSearchDetailsWidgetIconClass,
    SearchStudioSearchDetailsWidgetId,
    SearchStudioSearchDetailsWidgetLabel
} from '../search-studio-constants';

@injectable()
export class SearchStudioSearchDetailsWidget extends ReactWidget {

    @inject(SearchStudioSearchService)
    protected readonly _searchService!: SearchStudioSearchService;

    constructor() {
        super();
        this.id = SearchStudioSearchDetailsWidgetId;
        this.title.label = SearchStudioSearchDetailsWidgetLabel;
        this.title.caption = 'Search details';
        this.title.iconClass = SearchStudioSearchDetailsWidgetIconClass;
        this.title.closable = false;
        this.addClass('search-studio-search-details-widget');
        this.update();
    }

    @postConstruct()
    protected init(): void {
        this.toDispose.push(this._searchService.onDidChange(() => this.update()));
    }

    protected render(): React.ReactNode {
        const selectedResult = this._searchService.selectedResult;

        return (
            <div style={{
                display: 'grid',
                gap: '0.75rem',
                padding: '1rem',
                color: 'var(--theia-sideBar-foreground)'
            }}>
                <h2 style={{ margin: 0, fontSize: '1rem' }}>Details</h2>

                {!selectedResult ? (
                    <p style={{ margin: 0, color: 'var(--theia-descriptionForeground)', lineHeight: 1.6 }}>
                        Select a result to view details.
                    </p>
                ) : (
                    <div style={{ display: 'grid', gap: '0.75rem' }}>
                        <strong style={{ fontSize: '1rem' }}>{selectedResult.title}</strong>
                        <div style={{ display: 'grid', gap: '0.5rem' }}>
                            {this.renderField('Type', selectedResult.type)}
                            {this.renderField('Region', selectedResult.region)}
                            {this.renderField('Source', selectedResult.source)}
                            {this.renderField('Summary', selectedResult.summary)}
                        </div>
                    </div>
                )}
            </div>
        );
    }

    protected renderField(label: string, value: string): React.ReactNode {
        return (
            <div
                key={label}
                style={{
                    display: 'grid',
                    gap: '0.25rem',
                    padding: '0.75rem',
                    borderRadius: '6px',
                    border: '1px solid var(--theia-panel-border)',
                    background: 'var(--theia-editor-background)'
                }}
            >
                <span style={{ fontSize: '0.8rem', color: 'var(--theia-descriptionForeground)' }}>{label}</span>
                <span>{value}</span>
            </div>
        );
    }
}
