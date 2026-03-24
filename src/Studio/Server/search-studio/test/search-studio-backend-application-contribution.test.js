const assert = require('node:assert/strict');
const test = require('node:test');
const {
    SearchStudioRuntimeConfigurationEndpointPath,
    SearchStudioRuntimeConfigurationEnvironmentVariableName
} = require('../lib/common/search-studio-runtime-configuration.js');
const {
    SearchStudioBackendApplicationContribution
} = require('../lib/node/search-studio-backend-application-contribution.js');

/**
 * Verifies that the backend contribution exposes the same-origin runtime configuration endpoint with normalized values.
 */
test('configure registers the same-origin runtime configuration bridge', () => {
    // Track the route that the backend contribution registers so the endpoint contract stays protected.
    const originalEnvironmentValue = process.env[SearchStudioRuntimeConfigurationEnvironmentVariableName];
    process.env[SearchStudioRuntimeConfigurationEnvironmentVariableName] = 'https://localhost:7135/';

    let registeredRoute;
    let registeredHandler;

    try {
        // Capture the registered Express route without needing a full Theia backend host.
        const app = {
            get(route, handler) {
                registeredRoute = route;
                registeredHandler = handler;
            }
        };

        const contribution = new SearchStudioBackendApplicationContribution();
        contribution.configure(app);

        let payload;
        registeredHandler({}, {
            json(value) {
                payload = value;
            }
        });

        assert.equal(registeredRoute, SearchStudioRuntimeConfigurationEndpointPath);
        assert.equal(payload.studioApiHostBaseUrl, 'https://localhost:7135');
        assert.equal(payload.rawStudioApiHostBaseUrl, 'https://localhost:7135/');
        assert.equal(payload.environmentVariableName, SearchStudioRuntimeConfigurationEnvironmentVariableName);
    } finally {
        // Restore the previous process environment value so other tests do not inherit this configuration.
        if (originalEnvironmentValue === undefined) {
            delete process.env[SearchStudioRuntimeConfigurationEnvironmentVariableName];
        } else {
            process.env[SearchStudioRuntimeConfigurationEnvironmentVariableName] = originalEnvironmentValue;
        }
    }
});
