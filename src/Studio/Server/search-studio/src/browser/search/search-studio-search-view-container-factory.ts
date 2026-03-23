import { codicon, ViewContainer, ViewContainerTitleOptions, WidgetFactory, WidgetManager } from '@theia/core/lib/browser';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioSearchViewContainerId, SearchStudioSearchWidgetId } from '../search-studio-constants';

const SearchStudioSearchViewContainerTitleOptions: ViewContainerTitleOptions = {
    label: 'Search',
    iconClass: codicon('search'),
    closeable: true
};

@injectable()
export class SearchStudioSearchViewContainerFactory implements WidgetFactory {

    readonly id = SearchStudioSearchViewContainerId;

    protected readonly _searchWidgetOptions: ViewContainer.Factory.WidgetOptions = {
        order: 0,
        canHide: false,
        initiallyCollapsed: false,
        weight: 100,
        disableDraggingToOtherContainers: true
    };

    @inject(ViewContainer.Factory)
    protected readonly _viewContainerFactory!: ViewContainer.Factory;

    @inject(WidgetManager)
    protected readonly _widgetManager!: WidgetManager;

    async createWidget(): Promise<ViewContainer> {
        const viewContainer = this._viewContainerFactory({
            id: SearchStudioSearchViewContainerId,
            progressLocationId: SearchStudioSearchViewContainerId
        });

        viewContainer.setTitleOptions(SearchStudioSearchViewContainerTitleOptions);

        const searchWidget = await this._widgetManager.getOrCreateWidget(SearchStudioSearchWidgetId);
        viewContainer.addWidget(searchWidget, this._searchWidgetOptions);

        return viewContainer;
    }
}
