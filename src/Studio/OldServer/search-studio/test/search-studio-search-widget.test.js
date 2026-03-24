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

const { SearchStudioSearchWidget } = require('../lib/browser/search/search-studio-search-widget.js');

test('SearchStudioSearchWidget requests an initial render and stays non-closable', () => {
    const originalUpdate = SearchStudioSearchWidget.prototype.update;
    let updateCalls = 0;

    SearchStudioSearchWidget.prototype.update = function () {
        updateCalls += 1;
    };

    try {
        const widget = new SearchStudioSearchWidget();

        assert.equal(widget.title.label, 'Search');
        assert.equal(widget.title.closable, false);
        assert.equal(updateCalls, 1);
    } finally {
        SearchStudioSearchWidget.prototype.update = originalUpdate;
    }
});

test('SearchStudioSearchWidget routes input changes and Enter key presses through the same search path', () => {
    const originalUpdate = SearchStudioSearchWidget.prototype.update;

    SearchStudioSearchWidget.prototype.update = function () {};

    try {
        const widget = new SearchStudioSearchWidget();
        const calls = [];

        widget._searchService = {
            query: '',
            canSearch: true,
            hasSearched: false,
            setQuery(value) {
                calls.push({ type: 'setQuery', value });
            },
            requestSearch() {
                calls.push({ type: 'requestSearch' });
                return true;
            }
        };

        let prevented = false;

        widget.onQueryChanged('north sea');
        widget.onQueryKeyDown({
            key: 'Enter',
            preventDefault() {
                prevented = true;
            }
        });
        widget.onSearchTriggered();

        assert.equal(prevented, true);
        assert.deepEqual(calls, [
            { type: 'setQuery', value: 'north sea' },
            { type: 'requestSearch' },
            { type: 'requestSearch' }
        ]);
    } finally {
        SearchStudioSearchWidget.prototype.update = originalUpdate;
    }
});

test('SearchStudioSearchWidget ignores non-Enter keyboard input and Enter when search is disabled', () => {
    const originalUpdate = SearchStudioSearchWidget.prototype.update;

    SearchStudioSearchWidget.prototype.update = function () {};

    try {
        const widget = new SearchStudioSearchWidget();
        let requestCount = 0;

        widget._searchService = {
            query: '',
            canSearch: false,
            hasSearched: false,
            setQuery() {},
            requestSearch() {
                requestCount += 1;
                return false;
            }
        };

        widget.onQueryKeyDown({
            key: 'Escape',
            preventDefault() {
                throw new Error('preventDefault should not be called');
            }
        });
        widget.onQueryKeyDown({
            key: 'Enter',
            preventDefault() {
                throw new Error('preventDefault should not be called');
            }
        });

        assert.equal(requestCount, 0);
    } finally {
        SearchStudioSearchWidget.prototype.update = originalUpdate;
    }
});

test('SearchStudioSearchWidget hides facets until a search has been executed', () => {
    const originalUpdate = SearchStudioSearchWidget.prototype.update;

    SearchStudioSearchWidget.prototype.update = function () {};

    try {
        const widget = new SearchStudioSearchWidget();

        widget._searchService = {
            query: 'abc',
            canSearch: true,
            hasSearched: false,
            setQuery() {},
            requestSearch() {
                return true;
            }
        };

        const initialTree = JSON.stringify(widget.render());
        assert.doesNotMatch(initialTree, /Facets/);
        assert.doesNotMatch(initialTree, /Region/);

        widget._searchService.hasSearched = true;

        const searchedTree = JSON.stringify(widget.render());
        assert.match(searchedTree, /Facets/);
        assert.match(searchedTree, /Region/);
    } finally {
        SearchStudioSearchWidget.prototype.update = originalUpdate;
    }
});
