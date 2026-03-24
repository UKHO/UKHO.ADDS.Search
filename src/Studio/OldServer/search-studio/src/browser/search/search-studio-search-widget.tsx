import * as React from '@theia/core/shared/react';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { SearchStudioMockFacetGroups } from './search-studio-search-mock-data';
import { SearchStudioSearchService } from './search-studio-search-service';
import {
    SearchStudioSearchWidgetIconClass,
    SearchStudioSearchWidgetId,
    SearchStudioSearchWidgetLabel
} from '../search-studio-constants';
import { SearchStudioMockFacetGroup } from './search-studio-search-types';

@injectable()
export class SearchStudioSearchWidget extends ReactWidget {

    @inject(SearchStudioSearchService)
    protected readonly _searchService!: SearchStudioSearchService;

    constructor() {
        super();
        this.id = SearchStudioSearchWidgetId;
        this.title.label = SearchStudioSearchWidgetLabel;
        this.title.caption = 'Search navigation';
        this.title.iconClass = SearchStudioSearchWidgetIconClass;
        this.title.closable = false;
        this.addClass('search-studio-search-widget');
        this.update();
    }

    @postConstruct()
    protected init(): void {
        this.toDispose.push(this._searchService.onDidChange(() => this.update()));
    }

    protected render(): React.ReactNode {
        return (
            <div style={{
                display: 'grid',
                gap: '1rem',
                padding: '0.75rem',
                background: 'var(--theia-sideBar-background)',
                color: 'var(--theia-sideBar-foreground)'
            }}>
                <section style={{ display: 'grid', gap: '0.5rem' }}>
                    <div style={{ display: 'grid', gap: '0.5rem', gridTemplateColumns: 'minmax(0, 1fr) auto' }}>
                        <input
                            type="text"
                            value={this._searchService.query}
                            placeholder="Start typing to search the dataset"
                            aria-label="Search the dataset"
                            onChange={event => this.onQueryChanged(event.currentTarget.value)}
                            onKeyDown={event => this.onQueryKeyDown(event)}
                            style={{
                                width: '100%',
                                height: '2.25rem',
                                padding: '0.25rem 0.625rem',
                                borderRadius: '6px',
                                border: '1px solid var(--theia-input-border)',
                                background: 'var(--theia-input-background)',
                                color: 'var(--theia-input-foreground)',
                                boxSizing: 'border-box'
                            }}
                        />
                        <button
                            type="button"
                            className="theia-button"
                            disabled={!this._searchService.canSearch}
                            onClick={() => this.onSearchTriggered()}
                            style={{
                                minWidth: '5.5rem',
                                height: '2.25rem',
                                padding: '0.25rem 0.875rem',
                                borderRadius: '6px',
                                opacity: this._searchService.canSearch ? 1 : 0.6,
                                cursor: this._searchService.canSearch ? 'pointer' : 'not-allowed',
                                boxSizing: 'border-box'
                            }}
                        >
                            Search
                        </button>
                    </div>
                </section>

                {this._searchService.hasSearched ? (
                    <section style={{ display: 'grid', gap: '0.75rem' }}>
                        <h2 style={{ margin: 0, fontSize: '0.95rem' }}>Facets</h2>
                        <div style={{ display: 'grid', gap: '1rem' }}>
                            {SearchStudioMockFacetGroups.map(group => this.renderFacetGroup(group))}
                        </div>
                    </section>
                ) : undefined}
            </div>
        );
    }

    protected renderFacetGroup(group: SearchStudioMockFacetGroup): React.ReactNode {
        return (
            <section key={group.id} style={{ display: 'grid', gap: '0.5rem' }}>
                <header style={{
                    padding: '0.5rem 0.75rem',
                    borderRadius: '6px',
                    background: 'var(--theia-editorGroupHeader-tabsBackground)',
                    border: '1px solid var(--theia-panel-border)',
                    fontSize: '0.85rem',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '0.04em'
                }}>
                    {group.label}
                </header>
                <div style={{ display: 'grid', gap: '0.5rem' }}>
                    {group.options.map(option => (
                        <label
                            key={option.id}
                            style={{
                                display: 'flex',
                                alignItems: 'center',
                                gap: '0.5rem',
                                cursor: 'pointer'
                            }}
                        >
                            <input type="checkbox" />
                            <span>{option.label} ({option.count})</span>
                        </label>
                    ))}
                </div>
            </section>
        );
    }

    protected onQueryChanged(query: string): void {
        this._searchService.setQuery(query);
    }

    protected onSearchTriggered(): void {
        this._searchService.requestSearch();
    }

    protected onQueryKeyDown(event: React.KeyboardEvent<HTMLInputElement>): void {
        if (event.key !== 'Enter' || !this._searchService.canSearch) {
            return;
        }

        event.preventDefault();
        this.onSearchTriggered();
    }
}
