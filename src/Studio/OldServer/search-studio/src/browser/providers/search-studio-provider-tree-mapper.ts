import { CompositeTreeNode } from '@theia/core/lib/browser/tree/tree';
import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import {
    createSearchStudioCompositeTreeNode,
    createSearchStudioTreeLeafNode,
    createSearchStudioTreeRoot
} from '../common/search-studio-tree-types';
import {
    SearchStudioProviderCatalogSnapshot,
    SearchStudioProviderTreeNode
} from '../common/search-studio-shell-types';
import {
    SearchStudioProvidersCompositeTreeNode,
    SearchStudioProvidersTreeNode,
    SearchStudioProvidersTreeRoot
} from './search-studio-providers-tree-types';

export type SearchStudioProviderTreeOpenTarget = 'provider-overview' | 'provider-queue' | 'provider-dead-letters';

export function mapProviderDescriptorsToProviderTreeNodes(
    providers: readonly SearchStudioApiProviderDescriptor[]
): readonly SearchStudioProviderTreeNode[]
{
    return providers.map(provider => ({
        id: `provider:${provider.name}`,
        kind: 'provider-root',
        label: provider.displayName,
        description: provider.description,
        provider,
        badge: {
            value: 'API',
            title: 'Provider metadata loaded from StudioApiHost /providers.'
        },
        children: [
            {
                id: `provider:${provider.name}:queue`,
                kind: 'queue',
                label: 'Queue',
                description: 'Placeholder queue inspector surface.',
                provider,
                children: []
            },
            {
                id: `provider:${provider.name}:dead-letters`,
                kind: 'dead-letters',
                label: 'Dead letters',
                description: 'Placeholder dead-letter inspector surface.',
                provider,
                children: []
            }
        ]
    }));
}

export function mapProviderCatalogSnapshotToProvidersTreeRoot(
    snapshot: SearchStudioProviderCatalogSnapshot
): SearchStudioProvidersTreeRoot
{
    const root = createSearchStudioTreeRoot('search-studio.providers.root', 'Providers') as SearchStudioProvidersTreeRoot;
    const children = mapProviderCatalogSnapshotToTreeNodes(snapshot);

    CompositeTreeNode.addChildren(root, [...children]);

    return root;
}

export function resolveProviderTreeNodeOpenTarget(
    node: Pick<SearchStudioProvidersTreeNode, 'kind'>
): SearchStudioProviderTreeOpenTarget | undefined
{
    switch (node.kind) {
        case 'provider-root':
            return 'provider-overview';
        case 'queue':
            return 'provider-queue';
        case 'dead-letters':
            return 'provider-dead-letters';
        default:
            return undefined;
    }
}

function mapProviderCatalogSnapshotToTreeNodes(
    snapshot: SearchStudioProviderCatalogSnapshot
): readonly SearchStudioProvidersTreeNode[]
{
    switch (snapshot.status) {
        case 'loading':
            return [createStatusNode('search-studio.providers.status.loading', 'Loading Studio providers...', 'codicon codicon-loading')];
        case 'error':
            return [createStatusNode('search-studio.providers.status.error', snapshot.errorMessage ?? 'Studio provider metadata could not be loaded.', 'codicon codicon-error')];
        case 'ready':
            if (snapshot.providerNodes.length === 0) {
                return [createStatusNode('search-studio.providers.status.empty', 'No providers were returned by StudioApiHost.', 'codicon codicon-info')];
            }

            return snapshot.providerNodes.map(node => mapProviderTreeNodeToTreeNode(node));
        case 'idle':
        default:
            return [createStatusNode('search-studio.providers.status.idle', 'Loading Studio providers...', 'codicon codicon-loading')];
    }
}

function mapProviderTreeNodeToTreeNode(node: SearchStudioProviderTreeNode): SearchStudioProvidersTreeNode {
    const commonNodeProperties = {
        id: node.id,
        kind: node.kind,
        label: node.label,
        iconClass: getProviderTreeNodeIcon(node.kind),
        description: node.description,
        data: {
            provider: node.provider
        }
    } as const;

    if (node.children.length === 0) {
        return createSearchStudioTreeLeafNode(commonNodeProperties) as SearchStudioProvidersTreeNode;
    }

    return createSearchStudioCompositeTreeNode({
        ...commonNodeProperties,
        children: node.children.map(child => mapProviderTreeNodeToTreeNode(child))
    }) as SearchStudioProvidersCompositeTreeNode;
}

function createStatusNode(
    id: string,
    label: string,
    iconClass: string
): SearchStudioProvidersTreeNode
{
    return createSearchStudioTreeLeafNode({
        id,
        kind: 'status',
        label,
        iconClass
    }) as SearchStudioProvidersTreeNode;
}

function getProviderTreeNodeIcon(kind: SearchStudioProviderTreeNode['kind']): string {
    switch (kind) {
        case 'provider-root':
            return 'codicon codicon-database';
        case 'queue':
            return 'codicon codicon-list-unordered';
        case 'dead-letters':
            return 'codicon codicon-archive';
    }
}
