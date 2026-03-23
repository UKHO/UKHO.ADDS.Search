import { codicon, ViewContainer, ViewContainerTitleOptions, WidgetFactory, WidgetManager } from '@theia/core/lib/browser';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioIngestionViewContainerId, SearchStudioIngestionWidgetId } from '../search-studio-constants';

const SearchStudioIngestionViewContainerTitleOptions: ViewContainerTitleOptions = {
    label: 'Ingestion',
    iconClass: codicon('cloud-upload'),
    closeable: true
};

@injectable()
export class SearchStudioIngestionViewContainerFactory implements WidgetFactory {

    readonly id = SearchStudioIngestionViewContainerId;

    protected readonly _ingestionWidgetOptions: ViewContainer.Factory.WidgetOptions = {
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
            id: SearchStudioIngestionViewContainerId,
            progressLocationId: SearchStudioIngestionViewContainerId
        });

        viewContainer.setTitleOptions(SearchStudioIngestionViewContainerTitleOptions);

        const ingestionWidget = await this._widgetManager.getOrCreateWidget(SearchStudioIngestionWidgetId);
        viewContainer.addWidget(ingestionWidget, this._ingestionWidgetOptions);

        return viewContainer;
    }
}
