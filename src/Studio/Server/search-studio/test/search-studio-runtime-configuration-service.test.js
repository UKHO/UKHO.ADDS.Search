const assert = require('node:assert/strict');
const test = require('node:test');
const {
    SearchStudioRuntimeConfigurationEndpointPath
} = require('../lib/common/search-studio-runtime-configuration.js');
const {
    SearchStudioRuntimeConfigurationService
} = require('../lib/browser/search-studio-runtime-configuration-service.js');

/**
 * Verifies that the frontend runtime configuration service normalizes and caches the backend bridge payload.
 */
test('getConfiguration normalizes the Studio API base URL and caches the first successful response', async () => {
    // Replace the global fetch implementation so the service can be tested without a real Theia backend.
    const originalFetch = globalThis.fetch;
    const calls = [];
    globalThis.fetch = async (url, options) => {
        calls.push({ url, options });

        return {
            ok: true,
            status: 200,
            statusText: 'OK',
            async json() {
                return {
                    studioApiHostBaseUrl: ' https://localhost:7135/ ',
                    rawStudioApiHostBaseUrl: ' https://localhost:7135/ ',
                    environmentVariableName: 'STUDIO_API_HOST_API_BASE_URL'
                };
            }
        };
    };

    try {
        // Resolve the configuration twice so the test can prove the service only fetches it once.
        const service = new SearchStudioRuntimeConfigurationService();
        const firstConfiguration = await service.getConfiguration();
        const secondConfiguration = await service.getConfiguration();

        assert.equal(calls.length, 1);
        assert.equal(calls[0].url, SearchStudioRuntimeConfigurationEndpointPath);
        assert.equal(calls[0].options.method, 'GET');
        assert.equal(firstConfiguration.studioApiHostBaseUrl, 'https://localhost:7135');
        assert.equal(secondConfiguration.studioApiHostBaseUrl, 'https://localhost:7135');
    } finally {
        // Restore the previous global fetch implementation so later tests run in a clean environment.
        globalThis.fetch = originalFetch;
    }
});

/**
 * Verifies that the frontend runtime configuration service surfaces a descriptive error when the backend bridge fails.
 */
test('getConfiguration throws a descriptive error when the backend bridge request fails', async () => {
    // Replace the global fetch implementation so the failure path can be exercised deterministically.
    const originalFetch = globalThis.fetch;
    globalThis.fetch = async () => ({
        ok: false,
        status: 503,
        statusText: 'Service Unavailable'
    });

    try {
        // Assert that the service surfaces the failing HTTP status in the thrown error message.
        const service = new SearchStudioRuntimeConfigurationService();
        await assert.rejects(() => service.getConfiguration(), /503 Service Unavailable/);
    } finally {
        // Restore the previous global fetch implementation so later tests run in a clean environment.
        globalThis.fetch = originalFetch;
    }
});
