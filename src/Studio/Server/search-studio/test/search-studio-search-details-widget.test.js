const test = require('node:test');
const assert = require('node:assert/strict');

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

const { SearchStudioSearchDetailsWidget } = require('../lib/browser/search/search-studio-search-details-widget.js');
const { SearchStudioMockSearchResults } = require('../lib/browser/search/search-studio-search-mock-data.js');

test('SearchStudioSearchDetailsWidget renders the empty instructional state when nothing is selected', () => {
    const originalUpdate = SearchStudioSearchDetailsWidget.prototype.update;

    SearchStudioSearchDetailsWidget.prototype.update = function () {};

    try {
        const widget = new SearchStudioSearchDetailsWidget();

        widget._searchService = {
            selectedResult: undefined,
            onDidChange() {
                return {
                    dispose() {}
                };
            }
        };

        const tree = widget.render();
        const content = JSON.stringify(tree);

        assert.match(content, /Select a result to view details/);
    } finally {
        SearchStudioSearchDetailsWidget.prototype.update = originalUpdate;
    }
});

test('SearchStudioSearchDetailsWidget renders fake selected-result values when a result is selected', () => {
    const originalUpdate = SearchStudioSearchDetailsWidget.prototype.update;

    SearchStudioSearchDetailsWidget.prototype.update = function () {};

    try {
        const widget = new SearchStudioSearchDetailsWidget();

        widget._searchService = {
            selectedResult: SearchStudioMockSearchResults[0],
            onDidChange() {
                return {
                    dispose() {}
                };
            }
        };

        const tree = widget.render();
        const content = JSON.stringify(tree);

        assert.match(content, /Wreck - North Sea - Example 001/);
        assert.match(content, /Mock Hydrographic Dataset/);
        assert.match(content, /Illustrative mock detail content/);
    } finally {
        SearchStudioSearchDetailsWidget.prototype.update = originalUpdate;
    }
});
