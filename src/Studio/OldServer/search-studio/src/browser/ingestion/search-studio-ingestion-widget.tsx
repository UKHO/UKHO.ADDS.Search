import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ContextMenuRenderer } from '@theia/core/lib/browser/context-menu-renderer';
import { MenuPath } from '@theia/core/lib/common/menu';
import { TreeModel } from '@theia/core/lib/browser/tree/tree-model';
import { SelectableTreeNode } from '@theia/core/lib/browser/tree/tree-selection';
import { TreeProps } from '@theia/core/lib/browser/tree/tree-widget';
import { SearchStudioDocumentService } from '../common/search-studio-document-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import { SearchStudioTreeWidget } from '../common/search-studio-tree-widget';
import {
    SearchStudioIngestionModeContextMenuPath,
    SearchStudioIngestionRootContextMenuPath,
    SearchStudioIngestionWidgetIconClass,
    SearchStudioIngestionWidgetId,
    SearchStudioIngestionWidgetLabel
} from '../search-studio-constants';
import {
    resolveIngestionTreeNodeOpenTarget
} from './search-studio-ingestion-mapper';
import { SearchStudioIngestionTreeModel } from './search-studio-ingestion-tree-model';
import { isSearchStudioIngestionTreeNode } from './search-studio-ingestion-tree-types';

@injectable()
export class SearchStudioIngestionWidget extends SearchStudioTreeWidget {

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(SearchStudioIngestionTreeModel)
    protected readonly _ingestionTreeModel!: SearchStudioIngestionTreeModel;

    constructor(
        @inject(TreeProps) props: TreeProps,
        @inject(TreeModel) model: TreeModel,
        @inject(ContextMenuRenderer) contextMenuRenderer: ContextMenuRenderer
    )
    {
        super(props, model, contextMenuRenderer);
        this.id = SearchStudioIngestionWidgetId;
        this.title.label = SearchStudioIngestionWidgetLabel;
        this.title.caption = 'Ingestion navigation';
        this.title.iconClass = SearchStudioIngestionWidgetIconClass;
        this.title.closable = false;
        this.addClass('search-studio-ingestion-widget');
    }

    @postConstruct()
    protected init(): void {
        super.init();
        this._ingestionTreeModel.initializeModel();
        this.toDispose.push(this.model.onOpenNode(node => void this.openTreeNode(node)));
    }

    protected override toContextMenuArgs(node: SelectableTreeNode): any[] | undefined {
        if (!isSearchStudioIngestionTreeNode(node) || !node.provider) {
            return undefined;
        }

        switch (node.kind) {
            case 'provider-root':
                return [node.provider.name];
            case 'by-id':
            case 'all-unindexed':
            case 'by-context':
                return [node.provider.name, node.kind];
            default:
                return undefined;
        }
    }

    protected override getContextMenuPath(node: SelectableTreeNode): MenuPath | undefined {
        if (!isSearchStudioIngestionTreeNode(node)) {
            return undefined;
        }

        switch (node.kind) {
            case 'provider-root':
                return SearchStudioIngestionRootContextMenuPath;
            case 'by-id':
            case 'all-unindexed':
            case 'by-context':
                return SearchStudioIngestionModeContextMenuPath;
            default:
                return undefined;
        }
    }

    protected async openTreeNode(node: object): Promise<void> {
        if (!isSearchStudioIngestionTreeNode(node) || !node.provider) {
            return;
        }

        const openTarget = resolveIngestionTreeNodeOpenTarget(node);

        if (!openTarget) {
            return;
        }

        this._providerSelectionService.selectProvider(node.provider, 'ingestion');

        switch (openTarget) {
            case 'ingestion-overview':
                await this._documentService.openIngestionOverview(node.provider);
                return;
            case 'ingestion-by-id':
                await this._documentService.openIngestionById(node.provider);
                return;
            case 'ingestion-all-unindexed':
                await this._documentService.openIngestionAllUnindexed(node.provider);
                return;
            case 'ingestion-by-context':
                await this._documentService.openIngestionByContext(node.provider);
                return;
        }
    }
}
