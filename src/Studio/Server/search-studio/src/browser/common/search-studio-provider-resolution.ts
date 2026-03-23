import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';

export function resolvePreferredProvider(
    providers: readonly SearchStudioApiProviderDescriptor[],
    selectedProviderName?: string,
    explicitProviderName?: string
): SearchStudioApiProviderDescriptor | undefined
{
    if (explicitProviderName) {
        const explicitProvider = providers.find(provider => provider.name === explicitProviderName);

        if (explicitProvider) {
            return explicitProvider;
        }
    }

    if (selectedProviderName) {
        const selectedProvider = providers.find(provider => provider.name === selectedProviderName);

        if (selectedProvider) {
            return selectedProvider;
        }
    }

    return providers[0];
}
