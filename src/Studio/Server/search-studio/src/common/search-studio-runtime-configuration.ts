/**
 * Describes the runtime configuration that the Theia backend exposes to browser code.
 */
export interface SearchStudioRuntimeConfiguration {
    /**
     * Gets the normalized Studio API base URL without a trailing slash when it is available.
     */
    readonly studioApiHostBaseUrl?: string;

    /**
     * Gets the raw Studio API base URL value that the Theia backend received from Aspire.
     */
    readonly rawStudioApiHostBaseUrl?: string;

    /**
     * Gets the environment variable name that carried the Studio API base URL into the Theia process.
     */
    readonly environmentVariableName: string;
}

/**
 * Defines the environment variable name used by Aspire to pass the Studio API base URL into the shell process.
 */
export const SearchStudioRuntimeConfigurationEnvironmentVariableName = 'STUDIO_API_HOST_API_BASE_URL';

/**
 * Defines the same-origin backend endpoint that browser code uses to read runtime configuration.
 */
export const SearchStudioRuntimeConfigurationEndpointPath = '/search-studio/api/configuration';

/**
 * Normalizes a Studio API base URL value so browser and backend code share the same interpretation rules.
 *
 * @param studioApiHostBaseUrl The raw Studio API base URL value that should be normalized.
 * @returns The normalized base URL, or undefined when the input does not contain a usable value.
 */
export function normalizeStudioApiHostBaseUrl(studioApiHostBaseUrl?: string): string | undefined {
    // Trim whitespace first so accidental surrounding spaces do not leak into downstream URL composition.
    const trimmedStudioApiHostBaseUrl = studioApiHostBaseUrl?.trim();

    // Treat empty or whitespace-only configuration as missing so callers can handle it consistently.
    if (!trimmedStudioApiHostBaseUrl) {
        return undefined;
    }

    // Remove a single trailing slash because the browser later appends endpoint paths explicitly.
    return trimmedStudioApiHostBaseUrl.endsWith('/')
        ? trimmedStudioApiHostBaseUrl.slice(0, -1)
        : trimmedStudioApiHostBaseUrl;
}
