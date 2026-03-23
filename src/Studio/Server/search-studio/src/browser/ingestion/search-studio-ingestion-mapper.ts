import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import { SearchStudioIngestionNode, SearchStudioIngestionProviderGroup } from './search-studio-ingestion-types';

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
