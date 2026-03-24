import { CompositeTreeNode } from '@theia/core/lib/browser/tree/tree';
import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import {
    createSearchStudioCompositeTreeNode,
    createSearchStudioTreeLeafNode,
    createSearchStudioTreeRoot
} from '../common/search-studio-tree-types';
import { SearchStudioProviderCatalogSnapshot } from '../common/search-studio-shell-types';
import { SearchStudioIngestionNode, SearchStudioIngestionProviderGroup } from './search-studio-ingestion-types';
import {
    SearchStudioIngestionCompositeTreeNode,
    SearchStudioIngestionTreeNode,
    SearchStudioIngestionTreeRoot
} from './search-studio-ingestion-tree-types';

export type SearchStudioIngestionTreeOpenTarget = 'ingestion-overview' | 'ingestion-by-id' | 'ingestion-all-unindexed' | 'ingestion-by-context';

export function mapProvidersToIngestionProviderGroups(
    providers: readonly SearchStudioApiProviderDescriptor[]
): readonly SearchStudioIngestionProviderGroup[]
{
    return providers.map(provider => {
        const byIdNode: SearchStudioIngestionNode = {
            id: `ingestion:${provider.name}:by-id`,
            kind: 'by-id',
            label: 'By id',
            description: 'Targeted ingestion for a single document identifier.',
            provider,
            badge: {
                value: 'ID',
                title: 'Opens the by-id ingestion placeholder.'
            },
            children: []
        };

        const allUnindexedNode: SearchStudioIngestionNode = {
            id: `ingestion:${provider.name}:all-unindexed`,
            kind: 'all-unindexed',
            label: 'All unindexed',
            description: 'Bulk ingestion placeholder for remaining unindexed content.',
            provider,
            badge: {
                value: 'ALL',
                title: 'Opens the all-unindexed ingestion placeholder.'
            },
            children: []
        };

        const byContextNode: SearchStudioIngestionNode = {
            id: `ingestion:${provider.name}:by-context`,
            kind: 'by-context',
            label: 'By context',
            description: 'Context-based ingestion placeholder using Studio terminology.',
            provider,
            badge: {
                value: 'CTX',
                title: 'Opens the by-context ingestion placeholder.'
            },
            children: []
        };

        const rootNode: SearchStudioIngestionNode = {
            id: `ingestion:${provider.name}`,
            kind: 'provider-root',
            label: provider.displayName,
            description: provider.description,
            provider,
            badge: {
                value: '3',
                title: 'Three ingestion modes are available in the skeleton.'
            },
            children: [byIdNode, allUnindexedNode, byContextNode]
        };

        return {
            provider,
            rootNode,
            byIdNode,
            allUnindexedNode,
            byContextNode
        };
    });
}

export function mapProviderCatalogSnapshotToIngestionTreeRoot(
    snapshot: SearchStudioProviderCatalogSnapshot
): SearchStudioIngestionTreeRoot
{
    const root = createSearchStudioTreeRoot('search-studio.ingestion.root', 'Ingestion') as SearchStudioIngestionTreeRoot;
    const children = mapProviderCatalogSnapshotToTreeNodes(snapshot);

    CompositeTreeNode.addChildren(root, [...children]);

    return root;
}

export function resolveIngestionTreeNodeOpenTarget(
    node: Pick<SearchStudioIngestionTreeNode, 'kind'>
): SearchStudioIngestionTreeOpenTarget | undefined
{
    switch (node.kind) {
        case 'provider-root':
            return 'ingestion-overview';
        case 'by-id':
            return 'ingestion-by-id';
        case 'all-unindexed':
            return 'ingestion-all-unindexed';
        case 'by-context':
            return 'ingestion-by-context';
        default:
            return undefined;
    }
}

function mapProviderCatalogSnapshotToTreeNodes(
    snapshot: SearchStudioProviderCatalogSnapshot
): readonly SearchStudioIngestionTreeNode[]
{
    switch (snapshot.status) {
        case 'loading':
            return [createStatusNode('search-studio.ingestion.status.loading', 'Loading Studio providers...', 'codicon codicon-loading')];
        case 'error':
            return [createStatusNode('search-studio.ingestion.status.error', snapshot.errorMessage ?? 'Studio provider metadata could not be loaded.', 'codicon codicon-error')];
        case 'ready':
            if (snapshot.providers.length === 0) {
                return [createStatusNode('search-studio.ingestion.status.empty', 'No providers were returned by StudioApiHost.', 'codicon codicon-info')];
            }

            return mapProvidersToIngestionProviderGroups(snapshot.providers).map(group => mapIngestionNodeToTreeNode(group.rootNode));
        case 'idle':
        default:
            return [createStatusNode('search-studio.ingestion.status.idle', 'Loading Studio providers...', 'codicon codicon-loading')];
    }
}

function mapIngestionNodeToTreeNode(node: SearchStudioIngestionNode): SearchStudioIngestionTreeNode {
    const commonNodeProperties = {
        id: node.id,
        kind: node.kind,
        label: node.label,
        iconClass: getIngestionTreeNodeIcon(node.kind),
        description: node.description,
        data: {
            provider: node.provider,
            ingestionNode: node.kind === 'provider-root' ? undefined : node
        }
    } as const;

    if (node.children.length === 0) {
        return createSearchStudioTreeLeafNode(commonNodeProperties) as SearchStudioIngestionTreeNode;
    }

    return createSearchStudioCompositeTreeNode({
        ...commonNodeProperties,
        children: node.children.map(child => mapIngestionNodeToTreeNode(child))
    }) as SearchStudioIngestionCompositeTreeNode;
}

function createStatusNode(
    id: string,
    label: string,
    iconClass: string
): SearchStudioIngestionTreeNode
{
    return createSearchStudioTreeLeafNode({
        id,
        kind: 'status',
        label,
        iconClass
    }) as SearchStudioIngestionTreeNode;
}

function getIngestionTreeNodeIcon(kind: SearchStudioIngestionNode['kind']): string {
    switch (kind) {
        case 'provider-root':
            return 'codicon codicon-cloud-upload';
        case 'by-id':
            return 'codicon codicon-symbol-numeric';
        case 'all-unindexed':
            return 'codicon codicon-layers';
        case 'by-context':
            return 'codicon codicon-symbol-key';
    }
}
