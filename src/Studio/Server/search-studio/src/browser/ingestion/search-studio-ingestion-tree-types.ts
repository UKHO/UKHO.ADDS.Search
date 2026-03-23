import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import {
    SearchStudioCompositeTreeNode,
    SearchStudioTreeNode,
    SearchStudioTreeRoot,
    isSearchStudioTreeNode
} from '../common/search-studio-tree-types';
import { SearchStudioIngestionNode } from './search-studio-ingestion-types';

export type SearchStudioIngestionTreeNodeKind = 'provider-root' | 'by-id' | 'all-unindexed' | 'by-context' | 'status';

export interface SearchStudioIngestionTreeNode extends SearchStudioTreeNode {
    readonly kind: SearchStudioIngestionTreeNodeKind;
    readonly provider?: SearchStudioApiProviderDescriptor;
    readonly ingestionNode?: SearchStudioIngestionNode;
}

export type SearchStudioIngestionCompositeTreeNode = Omit<SearchStudioCompositeTreeNode, 'kind' | 'children'>
    & SearchStudioIngestionTreeNode
    & {
        children: SearchStudioIngestionTreeNode[];
    };

export interface SearchStudioIngestionTreeRoot extends SearchStudioTreeRoot {
    children: SearchStudioIngestionTreeNode[];
}

export function isSearchStudioIngestionTreeNode(node: unknown): node is SearchStudioIngestionTreeNode {
    if (!isSearchStudioTreeNode(node)) {
        return false;
    }

    switch (node.kind) {
        case 'provider-root':
        case 'by-id':
        case 'all-unindexed':
        case 'by-context':
        case 'status':
            return true;
        default:
            return false;
    }
}
