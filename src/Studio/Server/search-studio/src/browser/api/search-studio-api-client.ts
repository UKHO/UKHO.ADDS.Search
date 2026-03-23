import { injectable, inject } from '@theia/core/shared/inversify';
import { SearchStudioApiConfigurationService } from '../search-studio-api-configuration-service';
import { SearchStudioFrontendRequestTimeoutMilliseconds } from '../search-studio-future-api-configuration';
import { SearchStudioApiProviderDescriptor, SearchStudioApiRuleDiscoveryResponse } from './search-studio-api-types';

@injectable()
export class SearchStudioApiClient {

    @inject(SearchStudioApiConfigurationService)
    protected readonly _apiConfigurationService!: SearchStudioApiConfigurationService;

    async getProviders(): Promise<readonly SearchStudioApiProviderDescriptor[]> {
        return this.getJson<readonly SearchStudioApiProviderDescriptor[]>('/providers');
    }

    async getRules(): Promise<SearchStudioApiRuleDiscoveryResponse> {
        return this.getJson<SearchStudioApiRuleDiscoveryResponse>('/rules');
    }

    protected async getJson<T>(path: string): Promise<T> {
        const studioApiHostBaseUrl = (await this._apiConfigurationService.getConfiguration()).studioApiHostBaseUrl;

        if (!studioApiHostBaseUrl) {
            throw new Error('StudioApiHost base URL is not configured for the studio shell.');
        }

        const requestUrl = new URL(path, `${studioApiHostBaseUrl}/`).toString();
        const abortController = new AbortController();
        const timeout = window.setTimeout(() => abortController.abort(), SearchStudioFrontendRequestTimeoutMilliseconds);

        try {
            const response = await fetch(requestUrl, {
                method: 'GET',
                headers: {
                    Accept: 'application/json'
                },
                signal: abortController.signal
            });

            if (!response.ok) {
                throw new Error(`StudioApiHost request failed: ${response.status} ${response.statusText}`);
            }

            return await response.json() as T;
        } catch (error) {
            if (error instanceof Error && error.name === 'AbortError') {
                throw new Error(`StudioApiHost request timed out after ${SearchStudioFrontendRequestTimeoutMilliseconds}ms.`);
            }

            throw error;
        } finally {
            window.clearTimeout(timeout);
        }
    }
}
