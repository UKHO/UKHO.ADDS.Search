const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that Theia contribution imports expect when the tests run under Node.
global.navigator = { platform: 'Win32', userAgent: 'node.js' };
global.document = {
    createElement: () => ({ style: {}, classList: { add() {}, remove() {} }, setAttribute() {}, removeAttribute() {}, nodeType: 1, ownerDocument: null }),
    documentElement: { style: {} },
    body: { classList: { add() {}, remove() {} } },
    queryCommandSupported() {
        return false;
    },
    addEventListener() {},
    removeEventListener() {}
};
global.window = {
    navigator: global.navigator,
    document: global.document,
    localStorage: {
        getItem() {
            return undefined;
        },
        setItem() {},
        removeItem() {}
    }
};
global.HTMLElement = class HTMLElement {};
global.Element = global.HTMLElement;
global.DragEvent = class DragEvent {};
require.extensions['.css'] = () => {};

const {
    SearchStudioFrontendApplicationContribution
} = require('../lib/browser/search-studio-frontend-application-contribution.js');

/**
 * Verifies that startup logs the resolved runtime configuration without trying to attach Home before layout initialization.
 */
test('SearchStudioFrontendApplicationContribution logs successful startup configuration resolution', async () => {
    const events = [];
    const originalConsoleInfo = console.info;
    const originalConsoleWarn = console.warn;

    console.info = message => {
        events.push(message);
    };
    console.warn = message => {
        events.push(message);
    };

    try {
        // Use simple fake services so the test can observe the startup logging without a real Theia frontend.
        const contribution = new SearchStudioFrontendApplicationContribution(
            {
                async getConfiguration() {
                    events.push('configuration');
                    return {
                        studioApiHostBaseUrl: 'https://localhost:7135',
                        rawStudioApiHostBaseUrl: 'https://localhost:7135',
                        environmentVariableName: 'STUDIO_API_HOST_API_BASE_URL'
                    };
                }
            },
            {
                async openHome() {
                    events.push('home');
                }
            }
        );

        await contribution.onStart({});

        assert.deepEqual(events, [
            'configuration',
            'Studio runtime configuration resolved.'
        ]);
    } finally {
        // Restore console state so later tests keep the normal logging implementation.
        console.info = originalConsoleInfo;
        console.warn = originalConsoleWarn;
    }
});

/**
 * Verifies that startup surfaces runtime configuration failures without blocking the rest of the shell lifecycle.
 */
test('SearchStudioFrontendApplicationContribution logs startup configuration resolution failures', async () => {
    const events = [];
    const originalConsoleError = console.error;

    console.error = message => {
        events.push(message);
    };

    try {
        // Simulate a configuration failure and confirm the contribution logs it for diagnostics.
        const contribution = new SearchStudioFrontendApplicationContribution(
            {
                async getConfiguration() {
                    throw new Error('configuration failed');
                }
            },
            {
                async openHome() {
                    events.push('home');
                }
            }
        );

        await contribution.onStart({});

        assert.deepEqual(events, [
            'Failed to resolve Studio runtime configuration during startup.'
        ]);
    } finally {
        // Restore console state so later tests keep the normal logging implementation.
        console.error = originalConsoleError;
    }
});

/**
 * Verifies that Home opens only after the Theia layout is ready so the main-area tab can attach successfully.
 */
test('SearchStudioFrontendApplicationContribution opens Home during layout initialization', async () => {
    let openHomeCalls = 0;

    // Use simple fake services so the test can focus on the layout-phase Home open contract.
    const contribution = new SearchStudioFrontendApplicationContribution(
        {
            async getConfiguration() {
                throw new Error('not used');
            }
        },
        {
            async openHome() {
                openHomeCalls += 1;
            }
        }
    );

    await contribution.initializeLayout({});

    assert.equal(openHomeCalls, 1);
});
