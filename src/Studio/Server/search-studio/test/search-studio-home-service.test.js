const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that Theia service imports expect when the tests run under Node.
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

const { SearchStudioHomeService } = require('../lib/browser/home/search-studio-home-service.js');

/**
 * Verifies that the Home service attaches Home as a standard main-area document and reuses the same tab on reopen.
 */
test('SearchStudioHomeService opens Home in the main area and activates the tab', async () => {
    const widget = {
        id: 'search-studio.home',
        isAttached: false
    };
    const existingFirstWidget = {
        id: 'search-studio.document.providers.first'
    };
    const addWidgetCalls = [];
    const activateWidgetCalls = [];
    const originalConsoleInfo = console.info;
    const infoMessages = [];

    console.info = message => {
        infoMessages.push(message);
    };

    try {
        // Use lightweight fakes so the test can focus on the Home tab placement contract instead of real Theia services.
        const service = new SearchStudioHomeService(
            {
                mainPanel: {
                    *widgets() {
                        yield existingFirstWidget;
                    }
                },
                async addWidget(targetWidget, options) {
                    addWidgetCalls.push({ targetWidget, options });
                    targetWidget.isAttached = true;
                },
                async activateWidget(widgetId) {
                    activateWidgetCalls.push(widgetId);
                }
            },
            {
                async getOrCreateWidget(factoryId) {
                    assert.equal(factoryId, 'search-studio.home');
                    return widget;
                }
            }
        );

        await service.openHome();
        await service.openHome();

        assert.deepEqual(addWidgetCalls, [
            {
                targetWidget: widget,
                options: {
                    area: 'main',
                    mode: 'tab-before',
                    ref: existingFirstWidget
                }
            }
        ]);
        assert.deepEqual(activateWidgetCalls, ['search-studio.home', 'search-studio.home']);
        assert.deepEqual(infoMessages, ['Opened Studio Home.', 'Opened Studio Home.']);
    } finally {
        // Restore console state so later tests keep the normal logging implementation.
        console.info = originalConsoleInfo;
    }
});
