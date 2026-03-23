import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import {
    SearchStudioCompositeTreeNode,
    SearchStudioTreeNode,
    SearchStudioTreeRoot,
    isSearchStudioTreeNode
} from '../common/search-studio-tree-types';
import { SearchStudioRuleNode } from './search-studio-rules-types';

export type SearchStudioRulesTreeNodeKind = 'provider-root' | 'rule-checker' | 'rules-group' | 'rule' | 'status';

export interface SearchStudioRulesTreeNode extends SearchStudioTreeNode {
    readonly kind: SearchStudioRulesTreeNodeKind;
    readonly provider?: SearchStudioApiProviderDescriptor;
    readonly ruleNode?: SearchStudioRuleNode;
}

export type SearchStudioRulesCompositeTreeNode = Omit<SearchStudioCompositeTreeNode, 'kind' | 'children'>
    & SearchStudioRulesTreeNode
    & {
        children: SearchStudioRulesTreeNode[];
    };

export interface SearchStudioRulesTreeRoot extends SearchStudioTreeRoot {
    children: SearchStudioRulesTreeNode[];
}

export function isSearchStudioRulesTreeNode(node: unknown): node is SearchStudioRulesTreeNode {
    if (!isSearchStudioTreeNode(node)) {
        return false;
    }

    switch (node.kind) {
        case 'provider-root':
        case 'rule-checker':
        case 'rules-group':
        case 'rule':
        case 'status':
            return true;
        default:
            return false;
    }
}
