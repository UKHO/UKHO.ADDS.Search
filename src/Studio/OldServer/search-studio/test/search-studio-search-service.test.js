const test = require('node:test');
const assert = require('node:assert/strict');

const { SearchStudioSearchService } = require('../lib/browser/search/search-studio-search-service.js');
const { SearchStudioMockSearchResults } = require('../lib/browser/search/search-studio-search-mock-data.js');

test('SearchStudioSearchService enables search only when trimmed query text is present', () => {
    const service = new SearchStudioSearchService();
    let changeEvents = 0;

    service.onDidChange(() => {
        changeEvents += 1;
    });

    assert.equal(service.query, '');
    assert.equal(service.canSearch, false);
    assert.equal(service.hasSearched, false);

    service.setQuery('   ');

    assert.equal(service.query, '   ');
    assert.equal(service.canSearch, false);

    service.setQuery(' north sea ');

    assert.equal(service.query, ' north sea ');
    assert.equal(service.canSearch, true);
    assert.equal(changeEvents, 2);
});

test('SearchStudioSearchService emits trimmed query text when search is requested', () => {
    const service = new SearchStudioSearchService();
    const requests = [];

    service.onDidRequestSearch(event => {
        requests.push(event);
    });

    assert.equal(service.requestSearch(), false);

    service.setQuery('  baltic wreck  ');

    assert.equal(service.requestSearch(), true);
    assert.equal(service.hasSearched, true);
    assert.deepEqual(requests, [
        {
            query: 'baltic wreck'
        }
    ]);
});

test('SearchStudioSearchService tracks the selected mock result and clears it on a new search request', () => {
    const service = new SearchStudioSearchService();
    const changeSnapshots = [];

    service.onDidChange(() => {
        changeSnapshots.push(service.selectedResult?.id ?? null);
    });

    service.selectResult(SearchStudioMockSearchResults[0]);

    assert.equal(service.selectedResult?.id, 'result-001');

    service.setQuery('north sea');
    service.requestSearch();

    assert.equal(service.hasSearched, true);
    assert.equal(service.selectedResult, undefined);
    assert.deepEqual(changeSnapshots, ['result-001', 'result-001', null]);
});
