import { TreeModelImpl } from '@theia/core/lib/browser/tree/tree-model';
import { ExpandableTreeNode } from '@theia/core/lib/browser/tree/tree-expansion';
import { SelectableTreeNode } from '@theia/core/lib/browser/tree/tree-selection';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { SearchStudioProviderCatalogService } from '../api/search-studio-provider-catalog-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import {
    rememberTopLevelExpansionState,
    synchronizeTopLevelExpansionState
} from '../common/search-studio-top-level-expansion-state';
import { mapProviderCatalogSnapshotToIngestionTreeRoot } from './search-studio-ingestion-mapper';
import {
    isSearchStudioIngestionTreeNode,
    SearchStudioIngestionTreeRoot,
    SearchStudioIngestionTreeNode
} from './search-studio-ingestion-tree-types';

@injectable()
export class SearchStudioIngestionTreeModel extends TreeModelImpl {

    protected _initialized = false;
    protected readonly _topLevelExpansionState = new Map<string, boolean>();

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
        this.toDispose.push(this.onExpansionChanged(node => this.handleExpansionChanged(node)));

        this.rebuildTree();
        void this._providerCatalogService.ensureLoaded();
    }

    protected rebuildTree(): void {
        const snapshot = this._providerCatalogService.snapshot;

        this._providerSelectionService.synchronizeProviderSelection(snapshot.providers, 'ingestion');
        const root = mapProviderCatalogSnapshotToIngestionTreeRoot(snapshot);

        synchronizeTopLevelExpansionState(root, this._topLevelExpansionState);

        this.root = root;
        this.syncSelectedProvider();
    }

    protected handleExpansionChanged(node: Readonly<ExpandableTreeNode>): void {
        rememberTopLevelExpansionState(this.root as SearchStudioIngestionTreeRoot | undefined, node, this._topLevelExpansionState);
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
