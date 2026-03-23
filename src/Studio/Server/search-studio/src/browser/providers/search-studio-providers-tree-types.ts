import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import {
    SearchStudioCompositeTreeNode,
    SearchStudioTreeNode,
    SearchStudioTreeRoot,
    isSearchStudioTreeNode
} from '../common/search-studio-tree-types';

export type SearchStudioProvidersTreeNodeKind = 'provider-root' | 'queue' | 'dead-letters' | 'status';

export interface SearchStudioProvidersTreeNode extends SearchStudioTreeNode {
    readonly kind: SearchStudioProvidersTreeNodeKind;
    readonly provider?: SearchStudioApiProviderDescriptor;
}

export type SearchStudioProvidersCompositeTreeNode = Omit<SearchStudioCompositeTreeNode, 'kind' | 'children'>
    & SearchStudioProvidersTreeNode
    & {
        children: SearchStudioProvidersTreeNode[];
    };

export interface SearchStudioProvidersTreeRoot extends SearchStudioTreeRoot {
    children: SearchStudioProvidersTreeNode[];
}

export function isSearchStudioProvidersTreeNode(node: unknown): node is SearchStudioProvidersTreeNode {
    if (!isSearchStudioTreeNode(node)) {
        return false;
    }

    switch (node.kind) {
        case 'provider-root':
        case 'queue':
        case 'dead-letters':
        case 'status':
            return true;
        default:
            return false;
    }
}
