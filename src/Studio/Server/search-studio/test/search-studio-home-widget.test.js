const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that Theia widget imports expect when the tests run under Node.
global.navigator = { platform: 'Win32', userAgent: 'node.js' };
global.document = {
    createElement: () => ({
        style: {},
        classList: { add() {}, remove() {} },
        setAttribute() {},
        removeAttribute() {},
        appendChild() {},
        removeChild() {},
        addEventListener() {},
        removeEventListener() {},
        nodeType: 1,
        ownerDocument: null
    }),
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
require.extensions['.png'] = module => {
    module.exports = 'ukho-logo-transparent.png';
};

const { SearchStudioHomeWidget } = require('../lib/browser/home/search-studio-home-widget.js');

/**
 * Verifies that the restored Home widget remains a normal closable document tab and requests its first render.
 */
test('SearchStudioHomeWidget requests an initial render and stays closable', () => {
    const originalUpdate = SearchStudioHomeWidget.prototype.update;
    let updateCalls = 0;

    SearchStudioHomeWidget.prototype.update = function () {
        updateCalls += 1;
    };

    try {
        // Construct the widget once so the test can validate the restored Home tab contract.
        const widget = new SearchStudioHomeWidget();

        assert.equal(widget.id, 'search-studio.home');
        assert.equal(widget.title.label, 'Home');
        assert.equal(widget.title.closable, true);
        assert.equal(widget.title.iconClass, 'codicon codicon-home');
        assert.equal(updateCalls, 1);
    } finally {
        // Restore the original widget update method so later tests keep the real implementation.
        SearchStudioHomeWidget.prototype.update = originalUpdate;
    }
});
