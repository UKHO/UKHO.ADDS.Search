import { injectable } from '@theia/core/shared/inversify';
import {
    normalizeStudioApiHostBaseUrl,
    SearchStudioRuntimeConfiguration,
    SearchStudioRuntimeConfigurationEndpointPath
} from '../common/search-studio-runtime-configuration';

/**
 * Loads and caches the Studio runtime configuration that the Theia backend serves over the same-origin bridge.
 */
@injectable()
export class SearchStudioRuntimeConfigurationService {

    /**
     * Stores the resolved configuration once the first request succeeds.
     */
    protected configuration?: SearchStudioRuntimeConfiguration;

    /**
     * Stores the in-flight request so concurrent callers share one fetch.
     */
    protected configurationRequest?: Promise<SearchStudioRuntimeConfiguration>;

    /**
     * Gets the current Studio runtime configuration from the same-origin backend endpoint.
     *
     * @returns The resolved runtime configuration payload.
     */
    async getConfiguration(): Promise<SearchStudioRuntimeConfiguration> {
        // Return the cached configuration immediately after the first successful fetch.
        if (this.configuration) {
            return this.configuration;
        }

        // Reuse the first in-flight request so startup code does not issue duplicate configuration calls.
        if (!this.configurationRequest) {
            this.configurationRequest = this.fetchConfiguration();
        }

        return this.configurationRequest;
    }

    /**
     * Performs the actual configuration fetch against the Theia backend.
     *
     * @returns The normalized runtime configuration payload.
     */
    protected async fetchConfiguration(): Promise<SearchStudioRuntimeConfiguration> {
        // Call the same-origin backend endpoint so browser code never reads Node.js process environment directly.
        const response = await fetch(SearchStudioRuntimeConfigurationEndpointPath, {
            method: 'GET',
            headers: {
                Accept: 'application/json'
            }
        });

        // Surface a descriptive error so startup and smoke-test failures are diagnosable.
        if (!response.ok) {
            throw new Error(`Failed to load studio runtime configuration: ${response.status} ${response.statusText}`);
        }

        // Normalize the payload before caching it so every caller receives consistent values.
        const configuration = await response.json() as SearchStudioRuntimeConfiguration;
        this.configuration = {
            studioApiHostBaseUrl: normalizeStudioApiHostBaseUrl(configuration.studioApiHostBaseUrl),
            rawStudioApiHostBaseUrl: configuration.rawStudioApiHostBaseUrl,
            environmentVariableName: configuration.environmentVariableName
        };

        return this.configuration;
    }
}
