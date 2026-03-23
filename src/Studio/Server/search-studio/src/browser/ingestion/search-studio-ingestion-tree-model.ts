import { TreeModelImpl } from '@theia/core/lib/browser/tree/tree-model';
import { SelectableTreeNode } from '@theia/core/lib/browser/tree/tree-selection';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { SearchStudioProviderCatalogService } from '../api/search-studio-provider-catalog-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import { mapProviderCatalogSnapshotToIngestionTreeRoot } from './search-studio-ingestion-mapper';
import {
    isSearchStudioIngestionTreeNode,
    SearchStudioIngestionTreeNode
} from './search-studio-ingestion-tree-types';

@injectable()
export class SearchStudioIngestionTreeModel extends TreeModelImpl {

    protected _initialized = false;

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @postConstruct()
    protected override init(): void {
        super.init();

        this.initializeModel();
    }

    public initializeModel(): void {
        if (this._initialized) {
            return;
        }

        this._initialized = true;
        this.toDispose.push(this._providerCatalogService.onDidChange(() => this.rebuildTree()));
        this.toDispose.push(this._providerSelectionService.onDidChangeSelectedProvider(() => this.syncSelectedProvider()));
        this.toDispose.push(this.onSelectionChanged(nodes => this.handleSelectionChanged(nodes)));

        this.rebuildTree();
        void this._providerCatalogService.ensureLoaded();
    }

    protected rebuildTree(): void {
        const snapshot = this._providerCatalogService.snapshot;

        this._providerSelectionService.synchronizeProviderSelection(snapshot.providers, 'ingestion');
        this.root = mapProviderCatalogSnapshotToIngestionTreeRoot(snapshot);
        this.syncSelectedProvider();
    }

    protected handleSelectionChanged(nodes: readonly Readonly<SelectableTreeNode>[]): void {
        const selectedNode = nodes[0];

        if (!isSearchStudioIngestionTreeNode(selectedNode) || !selectedNode.provider) {
            return;
        }

        this._providerSelectionService.selectProvider(selectedNode.provider, 'ingestion');
    }

    protected syncSelectedProvider(): void {
        const providerName = this._providerSelectionService.selectedProviderName;

        if (!providerName) {
            return;
        }

        const providerNode = this.getNode(`ingestion:${providerName}`);

        if (!SelectableTreeNode.is(providerNode) || !isSearchStudioIngestionTreeNode(providerNode)) {
            return;
        }

        if (!providerNode.selected) {
            this.selectNode(providerNode as SearchStudioIngestionTreeNode);
        }
    }
}
