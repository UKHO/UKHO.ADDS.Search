import { ApplicationShell, FrontendApplication, FrontendApplicationContribution, WidgetManager } from '@theia/core/lib/browser';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { SearchStudioOutputService } from '../common/search-studio-output-service';
import {
    SearchStudioSearchResultsWidgetFactoryId,
    SearchStudioSearchResultsWidgetId
} from '../search-studio-constants';
import { SearchStudioSearchDetailsViewContribution } from './search-studio-search-details-view-contribution';
import { SearchStudioSearchResultsWidget } from './search-studio-search-results-widget';
import { SearchStudioSearchService } from './search-studio-search-service';
import { SearchStudioSearchRequestedEvent } from './search-studio-search-types';

@injectable()
export class SearchStudioSearchExecutionService implements FrontendApplicationContribution {

    @inject(SearchStudioSearchService)
    protected readonly _searchService!: SearchStudioSearchService;

    @inject(WidgetManager)
    protected readonly _widgetManager!: WidgetManager;

    @inject(ApplicationShell)
    protected readonly _shell!: ApplicationShell;

    @inject(SearchStudioSearchDetailsViewContribution)
    protected readonly _searchDetailsViewContribution!: SearchStudioSearchDetailsViewContribution;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    @postConstruct()
    init(): void {
        this._searchService.onDidRequestSearch(event => {
            void this.openResults(event);
        });
    }

    async initializeLayout(_app: FrontendApplication): Promise<void> {
    }

    protected async openResults(event: SearchStudioSearchRequestedEvent): Promise<void> {
        const widget = await this._widgetManager.getOrCreateWidget<SearchStudioSearchResultsWidget>(
            SearchStudioSearchResultsWidgetFactoryId);

        widget.setQuery(event.query);

        if (!widget.isAttached) {
            await this._shell.addWidget(widget, {
                area: 'main'
            });
        }

        await this._shell.activateWidget(SearchStudioSearchResultsWidgetId);
        await this._searchDetailsViewContribution.openView({ activate: false, reveal: true });
        this._outputService.info(`Opened search results for ${event.query}.`, 'search');
    }
}
