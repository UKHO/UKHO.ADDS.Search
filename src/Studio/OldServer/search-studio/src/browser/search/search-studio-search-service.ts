import { Emitter, Event } from '@theia/core/lib/common';
import { injectable } from '@theia/core/shared/inversify';
import {
    SearchStudioMockSearchResult,
    SearchStudioSearchRequestedEvent
} from './search-studio-search-types';

@injectable()
export class SearchStudioSearchService {

    protected readonly _onDidChange = new Emitter<void>();
    protected readonly _onDidRequestSearch = new Emitter<SearchStudioSearchRequestedEvent>();
    protected _query = '';
    protected _hasSearched = false;
    protected _selectedResult?: SearchStudioMockSearchResult;

    get query(): string {
        return this._query;
    }

    get canSearch(): boolean {
        return this.normalizedQuery.length > 0;
    }

    get hasSearched(): boolean {
        return this._hasSearched;
    }

    get selectedResult(): SearchStudioMockSearchResult | undefined {
        return this._selectedResult;
    }

    get onDidChange(): Event<void> {
        return this._onDidChange.event;
    }

    get onDidRequestSearch(): Event<SearchStudioSearchRequestedEvent> {
        return this._onDidRequestSearch.event;
    }

    setQuery(query: string): void {
        if (this._query === query) {
            return;
        }

        this._query = query;
        this._onDidChange.fire();
    }

    requestSearch(): boolean {
        if (!this.canSearch) {
            return false;
        }

        let didChange = false;

        if (!this._hasSearched) {
            this._hasSearched = true;
            didChange = true;
        }

        if (this._selectedResult) {
            this._selectedResult = undefined;
            didChange = true;
        }

        if (didChange) {
            this._onDidChange.fire();
        }

        this._onDidRequestSearch.fire({
            query: this.normalizedQuery
        });

        return true;
    }

    selectResult(result: SearchStudioMockSearchResult): void {
        if (this._selectedResult?.id === result.id) {
            return;
        }

        this._selectedResult = result;
        this._onDidChange.fire();
    }

    protected get normalizedQuery(): string {
        return this._query.trim();
    }
}
