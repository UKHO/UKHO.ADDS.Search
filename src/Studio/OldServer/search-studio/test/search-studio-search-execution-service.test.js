const test = require('node:test');
const assert = require('node:assert/strict');

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

const { SearchStudioSearchExecutionService } = require('../lib/browser/search/search-studio-search-execution-service.js');

test('SearchStudioSearchExecutionService opens search results and reveals the details panel when search is requested', async () => {
    const service = new SearchStudioSearchExecutionService();
    const resultWidget = {
        id: 'search-studio.search-results',
        isAttached: false,
        setQueryCalls: [],
        setQuery(query) {
            this.setQueryCalls.push(query);
        }
    };
    const addWidgetCalls = [];
    const activateWidgetCalls = [];
    const detailsCalls = [];
    const outputMessages = [];
    let searchHandler;

    service._searchService = {
        onDidRequestSearch(handler) {
            searchHandler = handler;
            return {
                dispose() {}
            };
        }
    };
    service._widgetManager = {
        async getOrCreateWidget(factoryId) {
            assert.equal(factoryId, 'search-studio.search-results');
            return resultWidget;
        }
    };
    service._shell = {
        async addWidget(widget, options) {
            addWidgetCalls.push({ widget, options });
            widget.isAttached = true;
        },
        async activateWidget(widgetId) {
            activateWidgetCalls.push(widgetId);
        }
    };
    service._searchDetailsViewContribution = {
        async openView(options) {
            detailsCalls.push(options);
        }
    };
    service._outputService = {
        info(message, source) {
            outputMessages.push({ message, source });
        }
    };

    service.init();
    assert.equal(typeof searchHandler, 'function');

    await service.openResults({ query: 'north sea wreck' });

    assert.deepEqual(resultWidget.setQueryCalls, ['north sea wreck']);
    assert.deepEqual(addWidgetCalls, [
        {
            widget: resultWidget,
            options: {
                area: 'main'
            }
        }
    ]);
    assert.equal(activateWidgetCalls.length, 1);
    assert.equal(activateWidgetCalls[0], 'search-studio.search-results');
    assert.deepEqual(detailsCalls, [
        {
            activate: false,
            reveal: true
        }
    ]);
    assert.deepEqual(outputMessages, [
        {
            message: 'Opened search results for north sea wreck.',
            source: 'search'
        }
    ]);
});
