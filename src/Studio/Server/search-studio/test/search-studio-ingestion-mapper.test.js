const test = require('node:test');
const assert = require('node:assert/strict');
const { mapProvidersToIngestionProviderGroups } = require('../lib/browser/ingestion/search-studio-ingestion-mapper.js');

test('mapProvidersToIngestionProviderGroups creates a provider root with the three agreed ingestion modes', () => {
    const groups = mapProvidersToIngestionProviderGroups([
        {
            name: 'file-share',
            displayName: 'File Share',
            description: 'Ingests content sourced from File Share.'
        }
    ]);

    assert.equal(groups.length, 1);
    assert.equal(groups[0].rootNode.kind, 'provider-root');
    assert.deepEqual(
        groups[0].rootNode.children.map(child => ({ kind: child.kind, label: child.label, badge: child.badge.value })),
        [
            { kind: 'by-id', label: 'By id', badge: 'ID' },
            { kind: 'all-unindexed', label: 'All unindexed', badge: 'ALL' },
            { kind: 'by-context', label: 'By context', badge: 'CTX' }
        ]);
});

test('mapProvidersToIngestionProviderGroups preserves provider metadata across ingestion nodes', () => {
    const [group] = mapProvidersToIngestionProviderGroups([
        {
            name: 'file-share',
            displayName: 'File Share',
            description: 'Ingests content sourced from File Share.'
        }
    ]);

    assert.equal(group.byIdNode.provider.name, 'file-share');
    assert.equal(group.allUnindexedNode.provider.displayName, 'File Share');
    assert.equal(group.byContextNode.provider.description, 'Ingests content sourced from File Share.');
});
