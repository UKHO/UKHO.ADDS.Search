import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { SearchStudioMockSearchResults } from './search-studio-search-mock-data';
import { SearchStudioSearchService } from './search-studio-search-service';
import { SearchStudioMockSearchResult } from './search-studio-search-types';
import {
    SearchStudioSearchResultsWidgetIconClass,
    SearchStudioSearchResultsWidgetId,
    SearchStudioSearchResultsWidgetLabel
} from '../search-studio-constants';

@injectable()
export class SearchStudioSearchResultsWidget extends ReactWidget {

    protected _activeQuery = '';

    @inject(SearchStudioSearchService)
    protected readonly _searchService!: SearchStudioSearchService;

    constructor() {
        super();
        this.id = SearchStudioSearchResultsWidgetId;
        this.title.label = SearchStudioSearchResultsWidgetLabel;
        this.title.caption = 'Search results';
        this.title.iconClass = SearchStudioSearchResultsWidgetIconClass;
        this.title.closable = true;
        this.addClass('search-studio-search-results-widget');
        this.update();
    }

    @postConstruct()
    protected init(): void {
        this.toDispose.push(this._searchService.onDidChange(() => this.update()));
    }

    setQuery(query: string): void {
        this._activeQuery = query;
        this.title.label = SearchStudioSearchResultsWidgetLabel;
        this.title.caption = `Search results for ${query}`;
        this.title.iconClass = SearchStudioSearchResultsWidgetIconClass;
        this.title.closable = true;
        this.update();
    }

    protected render(): React.ReactNode {
        return (
            <div style={{
                display: 'grid',
                gap: '1rem',
                padding: '1rem',
                maxWidth: '64rem'
            }}>
                <header style={{ display: 'grid', gap: '0.35rem' }}>
                    <h1 style={{ margin: 0 }}>{SearchStudioSearchResultsWidgetLabel}</h1>
                    <p style={{ margin: 0, color: 'var(--theia-descriptionForeground)' }}>
                        {this._activeQuery
                            ? `Showing static mock results for "${this._activeQuery}".`
                            : 'Showing static mock results.'}
                    </p>
                </header>
                <section style={{ display: 'grid', gap: '1rem' }}>
                    {SearchStudioMockSearchResults.map(result => (
                        <article
                            key={result.id}
                            role="button"
                            tabIndex={0}
                            aria-pressed={this._searchService.selectedResult?.id === result.id}
                            onClick={() => this.onResultSelected(result)}
                            onKeyDown={event => this.onResultKeyDown(event, result)}
                            style={{
                                display: 'grid',
                                gap: '0.75rem',
                                padding: '1.25rem 1.5rem',
                                borderRadius: '8px',
                                border: this._searchService.selectedResult?.id === result.id
                                    ? '1px solid var(--theia-focusBorder)'
                                    : '1px solid var(--theia-panel-border)',
                                background: this._searchService.selectedResult?.id === result.id
                                    ? 'var(--theia-editor-selectionBackground)'
                                    : 'var(--theia-editor-background)',
                                boxShadow: this._searchService.selectedResult?.id === result.id
                                    ? '0 0 0 1px var(--theia-focusBorder)'
                                    : '0 0 0 1px rgba(255, 255, 255, 0.02)',
                                cursor: 'pointer',
                                outline: 'none'
                            }}
                        >
                            <strong style={{ fontSize: '1.05rem' }}>{result.title}</strong>
                            <span style={{ color: 'var(--theia-descriptionForeground)' }}>
                                {result.type} | {result.region}
                            </span>
                            <div style={{
                                height: '1px',
                                background: 'var(--theia-panel-border)'
                            }} />
                        </article>
                    ))}
                </section>
                <footer style={{ color: 'var(--theia-descriptionForeground)' }}>
                    {SearchStudioMockSearchResults.length} results | 2 ms
                </footer>
            </div>
        );
    }

    protected onResultSelected(result: SearchStudioMockSearchResult): void {
        this._searchService.selectResult(result);
    }

    protected onResultKeyDown(event: React.KeyboardEvent<HTMLElement>, result: SearchStudioMockSearchResult): void {
        if (event.key !== 'Enter' && event.key !== ' ') {
            return;
        }

        event.preventDefault();
        this.onResultSelected(result);
    }
}
