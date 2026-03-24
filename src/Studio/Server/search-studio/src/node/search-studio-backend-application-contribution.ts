import * as express from 'express';
import { BackendApplicationContribution } from '@theia/core/lib/node';
import { injectable } from '@theia/core/shared/inversify';
import {
    normalizeStudioApiHostBaseUrl,
    SearchStudioRuntimeConfiguration,
    SearchStudioRuntimeConfigurationEndpointPath,
    SearchStudioRuntimeConfigurationEnvironmentVariableName
} from '../common/search-studio-runtime-configuration';

/**
 * Exposes the Studio runtime configuration to browser code through a same-origin backend endpoint.
 */
@injectable()
export class SearchStudioBackendApplicationContribution implements BackendApplicationContribution {

    /**
     * Adds the Studio runtime configuration endpoint to the Theia backend application.
     *
     * @param app The Express application that hosts the Theia backend endpoints.
     */
    configure(app: express.Application): void {
        // Map the same-origin configuration bridge so browser code can read the Aspire-provided Studio API base URL safely.
        app.get(SearchStudioRuntimeConfigurationEndpointPath, (_request, response) => {
            const rawStudioApiHostBaseUrl = process.env[SearchStudioRuntimeConfigurationEnvironmentVariableName];
            const configuration: SearchStudioRuntimeConfiguration = {
                studioApiHostBaseUrl: normalizeStudioApiHostBaseUrl(rawStudioApiHostBaseUrl),
                rawStudioApiHostBaseUrl,
                environmentVariableName: SearchStudioRuntimeConfigurationEnvironmentVariableName
            };

            if (configuration.studioApiHostBaseUrl) {
                // Log the resolved value so local startup diagnostics show which Studio API endpoint Aspire handed off.
                console.info('Resolved Studio runtime configuration for the Theia backend.', configuration);
            } else {
                // Warn instead of failing the route so the shell can still render a diagnosable bootstrap state.
                console.warn('Studio runtime configuration is missing the Studio API base URL environment variable.', {
                    environmentVariableName: SearchStudioRuntimeConfigurationEnvironmentVariableName
                });
            }

            response.json(configuration);
        });
    }
}
