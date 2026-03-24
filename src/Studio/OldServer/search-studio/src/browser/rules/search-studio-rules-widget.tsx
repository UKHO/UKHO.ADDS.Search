import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ContextMenuRenderer } from '@theia/core/lib/browser/context-menu-renderer';
import { TreeModel } from '@theia/core/lib/browser/tree/tree-model';
import { SelectableTreeNode } from '@theia/core/lib/browser/tree/tree-selection';
import { TreeProps } from '@theia/core/lib/browser/tree/tree-widget';
import { SearchStudioProviderCatalogService } from '../api/search-studio-provider-catalog-service';
import { SearchStudioDocumentService } from '../common/search-studio-document-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import { SearchStudioTreeWidget } from '../common/search-studio-tree-widget';
import {
    SearchStudioRulesWidgetIconClass,
    SearchStudioRulesWidgetId,
    SearchStudioRulesWidgetLabel
} from '../search-studio-constants';
import {
    resolveRulesTreeNodeOpenTarget
} from './search-studio-rules-mapper';
import { SearchStudioRulesTreeModel } from './search-studio-rules-tree-model';
import { isSearchStudioRulesTreeNode } from './search-studio-rules-tree-types';

@injectable()
export class SearchStudioRulesWidget extends SearchStudioTreeWidget {

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    @inject(SearchStudioRulesTreeModel)
    protected readonly _rulesTreeModel!: SearchStudioRulesTreeModel;

    constructor(
        @inject(TreeProps) props: TreeProps,
        @inject(TreeModel) model: TreeModel,
        @inject(ContextMenuRenderer) contextMenuRenderer: ContextMenuRenderer
    )
    {
        super(props, model, contextMenuRenderer);
        this.id = SearchStudioRulesWidgetId;
        this.title.label = SearchStudioRulesWidgetLabel;
        this.title.caption = 'Rules navigation';
        this.title.iconClass = SearchStudioRulesWidgetIconClass;
        this.title.closable = false;
        this.addClass('search-studio-rules-widget');
    }

    @postConstruct()
    protected init(): void {
        super.init();
        this._rulesTreeModel.initializeModel();
        this.toDispose.push(this.model.onOpenNode(node => void this.openTreeNode(node)));
    }

    protected override toContextMenuArgs(node: SelectableTreeNode): any[] | undefined {
        if (!isSearchStudioRulesTreeNode(node) || !node.provider) {
            return undefined;
        }

        return [node.provider.name];
    }

    protected async openTreeNode(node: object): Promise<void> {
        if (!isSearchStudioRulesTreeNode(node) || !node.provider) {
            return;
        }

        const openTarget = resolveRulesTreeNodeOpenTarget(node);

        if (!openTarget) {
            return;
        }

        this._providerSelectionService.selectProvider(node.provider, 'rules');

        switch (openTarget) {
            case 'rules-overview':
                await this._documentService.openRulesOverview(node.provider);
                return;
            case 'rule-checker':
                await this._documentService.openRuleChecker(node.provider);
                return;
            case 'rule-editor':
                if (node.ruleNode) {
                    await this._documentService.openRuleEditor(node.provider, node.ruleNode);
                }
                return;
        }
    }
}
