import { TreeModelImpl } from '@theia/core/lib/browser/tree/tree-model';
import { ExpandableTreeNode } from '@theia/core/lib/browser/tree/tree-expansion';
import { SelectableTreeNode } from '@theia/core/lib/browser/tree/tree-selection';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { SearchStudioRulesCatalogService } from '../api/search-studio-rules-catalog-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import {
    rememberTopLevelExpansionState,
    synchronizeTopLevelExpansionState
} from '../common/search-studio-top-level-expansion-state';
import {
    mapRulesCatalogSnapshotToRulesTreeRoot
} from './search-studio-rules-mapper';
import {
    isSearchStudioRulesTreeNode,
    SearchStudioRulesTreeRoot,
    SearchStudioRulesTreeNode
} from './search-studio-rules-tree-types';

@injectable()
export class SearchStudioRulesTreeModel extends TreeModelImpl {

    protected _initialized = false;
    protected readonly _topLevelExpansionState = new Map<string, boolean>();

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
        this.toDispose.push(this.onExpansionChanged(node => this.handleExpansionChanged(node)));

        this.rebuildTree();
        void this._rulesCatalogService.ensureLoaded();
    }

    protected rebuildTree(): void {
        const snapshot = this._rulesCatalogService.snapshot;
        const providers = snapshot.providers.map(group => group.provider);

        this._providerSelectionService.synchronizeProviderSelection(providers, 'rules');
        const root = mapRulesCatalogSnapshotToRulesTreeRoot(snapshot);

        synchronizeTopLevelExpansionState(root, this._topLevelExpansionState);

        this.root = root;
        this.syncSelectedProvider();
    }

    protected handleExpansionChanged(node: Readonly<ExpandableTreeNode>): void {
        rememberTopLevelExpansionState(this.root as SearchStudioRulesTreeRoot | undefined, node, this._topLevelExpansionState);
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
