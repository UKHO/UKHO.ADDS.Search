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

const { SearchStudioSearchResultsWidget } = require('../lib/browser/search/search-studio-search-results-widget.js');
const { SearchStudioMockSearchResults } = require('../lib/browser/search/search-studio-search-mock-data.js');

test('SearchStudioSearchResultsWidget updates its tab title when the active query changes', () => {
    const originalUpdate = SearchStudioSearchResultsWidget.prototype.update;
    let updateCalls = 0;

    SearchStudioSearchResultsWidget.prototype.update = function () {
        updateCalls += 1;
    };

    try {
        const widget = new SearchStudioSearchResultsWidget();

        widget.setQuery('north sea');

        assert.equal(widget.title.label, 'Search results');
        assert.equal(widget.title.caption, 'Search results for north sea');
        assert.equal(widget.title.closable, true);
        assert.equal(updateCalls, 2);
    } finally {
        SearchStudioSearchResultsWidget.prototype.update = originalUpdate;
    }
});

test('SearchStudioSearchResultsWidget selects a result through the shared search service', () => {
    const originalUpdate = SearchStudioSearchResultsWidget.prototype.update;
    const selected = [];

    SearchStudioSearchResultsWidget.prototype.update = function () {};

    try {
        const widget = new SearchStudioSearchResultsWidget();

        widget._searchService = {
            selectedResult: undefined,
            onDidChange() {
                return {
                    dispose() {}
                };
            },
            selectResult(result) {
                selected.push(result.id);
            }
        };

        widget.onResultSelected(SearchStudioMockSearchResults[2]);

        assert.deepEqual(selected, ['result-003']);
    } finally {
        SearchStudioSearchResultsWidget.prototype.update = originalUpdate;
    }
});
