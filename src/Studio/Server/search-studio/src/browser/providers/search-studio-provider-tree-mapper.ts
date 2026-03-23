import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import { SearchStudioProviderTreeNode } from '../common/search-studio-shell-types';

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
