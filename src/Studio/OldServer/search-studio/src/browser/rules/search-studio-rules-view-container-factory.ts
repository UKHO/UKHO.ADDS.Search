import { codicon, ViewContainer, ViewContainerTitleOptions, WidgetFactory, WidgetManager } from '@theia/core/lib/browser';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioRulesViewContainerId, SearchStudioRulesWidgetId } from '../search-studio-constants';

const SearchStudioRulesViewContainerTitleOptions: ViewContainerTitleOptions = {
    label: 'Rules',
    iconClass: codicon('symbol-field'),
    closeable: true
};

@injectable()
export class SearchStudioRulesViewContainerFactory implements WidgetFactory {

    readonly id = SearchStudioRulesViewContainerId;

    protected readonly _rulesWidgetOptions: ViewContainer.Factory.WidgetOptions = {
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
            id: SearchStudioRulesViewContainerId,
            progressLocationId: SearchStudioRulesViewContainerId
        });

        viewContainer.setTitleOptions(SearchStudioRulesViewContainerTitleOptions);

        const rulesWidget = await this._widgetManager.getOrCreateWidget(SearchStudioRulesWidgetId);
        viewContainer.addWidget(rulesWidget, this._rulesWidgetOptions);

        return viewContainer;
    }
}
