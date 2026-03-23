const test = require('node:test');
const assert = require('node:assert/strict');
const { SearchStudioProviderSelectionService } = require('../lib/browser/common/search-studio-provider-selection-service.js');

function createService() {
    const service = new SearchStudioProviderSelectionService();
    service._outputService = { info: () => undefined };
    return service;
}

test('synchronizeProviderSelection selects the first provider when none is selected', () => {
    const service = createService();

    service.synchronizeProviderSelection([
        { name: 'file-share', displayName: 'File Share' },
        { name: 'other-provider', displayName: 'Other Provider' }
    ], 'providers');

    assert.equal(service.selectedProviderName, 'file-share');
});

test('synchronizeProviderSelection keeps the current provider when it is still present and falls back when it is removed', () => {
    const service = createService();

    service.selectProvider({ name: 'other-provider', displayName: 'Other Provider' }, 'rules');
    service.synchronizeProviderSelection([
        { name: 'file-share', displayName: 'File Share' },
        { name: 'other-provider', displayName: 'Other Provider' }
    ], 'ingestion');

    assert.equal(service.selectedProviderName, 'other-provider');

    service.synchronizeProviderSelection([
        { name: 'file-share', displayName: 'File Share' }
    ], 'ingestion');

    assert.equal(service.selectedProviderName, 'file-share');
});
