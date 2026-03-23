import { codicon, ViewContainer, ViewContainerTitleOptions, WidgetFactory, WidgetManager } from '@theia/core/lib/browser';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioProvidersViewContainerId, SearchStudioWidgetId } from '../search-studio-constants';

const SearchStudioProvidersViewContainerTitleOptions: ViewContainerTitleOptions = {
    label: 'Providers',
    iconClass: codicon('database'),
    closeable: true
};

@injectable()
export class SearchStudioProvidersViewContainerFactory implements WidgetFactory {

    readonly id = SearchStudioProvidersViewContainerId;

    protected readonly _providersWidgetOptions: ViewContainer.Factory.WidgetOptions = {
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
            id: SearchStudioProvidersViewContainerId,
            progressLocationId: SearchStudioProvidersViewContainerId
        });

        viewContainer.setTitleOptions(SearchStudioProvidersViewContainerTitleOptions);

        const providersWidget = await this._widgetManager.getOrCreateWidget(SearchStudioWidgetId);
        viewContainer.addWidget(providersWidget, this._providersWidgetOptions);

        return viewContainer;
    }
}
