import type { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import type { SearchStudioNodeBadge } from '../common/search-studio-shell-types';

export type SearchStudioIngestionNodeKind = 'provider-root' | 'by-id' | 'all-unindexed' | 'by-context';

export interface SearchStudioIngestionNode {
    readonly id: string;
    readonly kind: SearchStudioIngestionNodeKind;
    readonly label: string;
    readonly description?: string;
    readonly provider: SearchStudioApiProviderDescriptor;
    readonly badge?: SearchStudioNodeBadge;
    readonly children: readonly SearchStudioIngestionNode[];
}

export interface SearchStudioIngestionProviderGroup {
    readonly provider: SearchStudioApiProviderDescriptor;
    readonly rootNode: SearchStudioIngestionNode;
    readonly byIdNode: SearchStudioIngestionNode;
    readonly allUnindexedNode: SearchStudioIngestionNode;
    readonly byContextNode: SearchStudioIngestionNode;
}
