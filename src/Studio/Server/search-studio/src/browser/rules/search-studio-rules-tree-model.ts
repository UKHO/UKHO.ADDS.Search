import { TreeModelImpl } from '@theia/core/lib/browser/tree/tree-model';
import { SelectableTreeNode } from '@theia/core/lib/browser/tree/tree-selection';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { SearchStudioRulesCatalogService } from '../api/search-studio-rules-catalog-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import {
    mapRulesCatalogSnapshotToRulesTreeRoot
} from './search-studio-rules-mapper';
import {
    isSearchStudioRulesTreeNode,
    SearchStudioRulesTreeNode
} from './search-studio-rules-tree-types';

@injectable()
export class SearchStudioRulesTreeModel extends TreeModelImpl {

    protected _initialized = false;

    @inject(SearchStudioRulesCatalogService)
    protected readonly _rulesCatalogService!: SearchStudioRulesCatalogService;

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
        this.toDispose.push(this._rulesCatalogService.onDidChange(() => this.rebuildTree()));
        this.toDispose.push(this._providerSelectionService.onDidChangeSelectedProvider(() => this.syncSelectedProvider()));
        this.toDispose.push(this.onSelectionChanged(nodes => this.handleSelectionChanged(nodes)));

        this.rebuildTree();
        void this._rulesCatalogService.ensureLoaded();
    }

    protected rebuildTree(): void {
        const snapshot = this._rulesCatalogService.snapshot;
        const providers = snapshot.providers.map(group => group.provider);

        this._providerSelectionService.synchronizeProviderSelection(providers, 'rules');
        this.root = mapRulesCatalogSnapshotToRulesTreeRoot(snapshot);
        this.syncSelectedProvider();
    }

    protected handleSelectionChanged(nodes: readonly Readonly<SelectableTreeNode>[]): void {
        const selectedNode = nodes[0];

        if (!isSearchStudioRulesTreeNode(selectedNode) || !selectedNode.provider) {
            return;
        }

        this._providerSelectionService.selectProvider(selectedNode.provider, 'rules');
    }

    protected syncSelectedProvider(): void {
        const providerName = this._providerSelectionService.selectedProviderName;

        if (!providerName) {
            return;
        }

        const providerNode = this.getNode(`rules:${providerName}`);

        if (!SelectableTreeNode.is(providerNode) || !isSearchStudioRulesTreeNode(providerNode)) {
            return;
        }

        if (!providerNode.selected) {
            this.selectNode(providerNode as SearchStudioRulesTreeNode);
        }
    }
}
