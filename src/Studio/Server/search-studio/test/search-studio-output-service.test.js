const test = require('node:test');
const assert = require('node:assert/strict');
const { SearchStudioOutputService } = require('../lib/browser/common/search-studio-output-service.js');

test('SearchStudioOutputService clear removes entries and raises change events', () => {
    const service = new SearchStudioOutputService();
    let changeCount = 0;

    service.onDidChangeEntries(() => {
        changeCount += 1;
    });

    service.info('Loaded providers.', 'providers');
    service.error('Rules endpoint failed.', 'rules');

    assert.equal(service.entries.length, 2);

    service.clear();

    assert.deepEqual(service.entries, []);
    assert.equal(changeCount, 3);
});

test('SearchStudioOutputService appends new entries in natural reading order while preserving source metadata', () => {
    const service = new SearchStudioOutputService();

    service.info('Loaded providers.', 'providers');
    service.error('Rules endpoint failed.', 'rules');

    assert.deepEqual(
        service.entries.map(entry => ({ level: entry.level, source: entry.source, message: entry.message })),
        [
            { level: 'info', source: 'providers', message: 'Loaded providers.' },
            { level: 'error', source: 'rules', message: 'Rules endpoint failed.' }
        ]);
});

test('SearchStudioOutputService appends new output after clear without reviving earlier entries', () => {
    const service = new SearchStudioOutputService();

    service.info('Loaded providers.', 'providers');
    service.error('Rules endpoint failed.', 'rules');
    service.clear();
    service.info('Refresh completed.', 'providers');

    assert.deepEqual(
        service.entries.map(entry => ({ level: entry.level, source: entry.source, message: entry.message })),
        [
            { level: 'info', source: 'providers', message: 'Refresh completed.' }
        ]);
});
