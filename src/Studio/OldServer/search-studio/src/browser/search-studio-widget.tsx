import { ContextMenuRenderer } from '@theia/core/lib/browser/context-menu-renderer';
import { TreeModel } from '@theia/core/lib/browser/tree/tree-model';
import { SelectableTreeNode } from '@theia/core/lib/browser/tree/tree-selection';
import { TreeProps } from '@theia/core/lib/browser/tree/tree-widget';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { SearchStudioDocumentService } from './common/search-studio-document-service';
import { SearchStudioProviderSelectionService } from './common/search-studio-provider-selection-service';
import { SearchStudioTreeWidget } from './common/search-studio-tree-widget';
import { SearchStudioProviderTreeModel } from './providers/search-studio-provider-tree-model';
import {
    resolveProviderTreeNodeOpenTarget
} from './providers/search-studio-provider-tree-mapper';
import { isSearchStudioProvidersTreeNode } from './providers/search-studio-providers-tree-types';
import {
    SearchStudioWidgetIconClass,
    SearchStudioWidgetId,
    SearchStudioWidgetLabel
} from './search-studio-constants';

@injectable()
export class SearchStudioWidget extends SearchStudioTreeWidget {

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(SearchStudioProviderTreeModel)
    protected readonly _providerTreeModel!: SearchStudioProviderTreeModel;

    constructor(
        @inject(TreeProps) props: TreeProps,
        @inject(TreeModel) model: TreeModel,
        @inject(ContextMenuRenderer) contextMenuRenderer: ContextMenuRenderer
    )
    {
        super(props, model, contextMenuRenderer);
        this.id = SearchStudioWidgetId;
        this.title.label = SearchStudioWidgetLabel;
        this.title.caption = 'Providers navigation';
        this.title.iconClass = SearchStudioWidgetIconClass;
        this.title.closable = false;
        this.addClass('search-studio-providers-tree-widget');
    }

    @postConstruct()
    protected override init(): void {
        super.init();
        this._providerTreeModel.initializeModel();
        this.toDispose.push(this.model.onOpenNode(node => void this.openTreeNode(node)));
    }

    protected override toContextMenuArgs(node: SelectableTreeNode): any[] | undefined {
        if (!isSearchStudioProvidersTreeNode(node) || !node.provider) {
            return undefined;
        }

        return [node.provider.name];
    }

    protected async openTreeNode(node: object): Promise<void> {
        if (!isSearchStudioProvidersTreeNode(node) || !node.provider) {
            return;
        }

        const openTarget = resolveProviderTreeNodeOpenTarget(node);

        if (!openTarget) {
            return;
        }

        this._providerSelectionService.selectProvider(node.provider, 'providers');

        switch (openTarget) {
            case 'provider-overview':
                await this._documentService.openProviderOverview(node.provider);
                return;
            case 'provider-queue':
                await this._documentService.openProviderQueue(node.provider);
                return;
            case 'provider-dead-letters':
                await this._documentService.openProviderDeadLetters(node.provider);
                return;
        }
    }
}
