const test = require('node:test');
const assert = require('node:assert/strict');
const {
    mapProvidersToIngestionProviderGroups,
    mapProviderCatalogSnapshotToIngestionTreeRoot,
    resolveIngestionTreeNodeOpenTarget
} = require('../lib/browser/ingestion/search-studio-ingestion-mapper.js');

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

test('mapProviderCatalogSnapshotToIngestionTreeRoot maps provider roots and ingestion modes into a native tree shape', () => {
    const root = mapProviderCatalogSnapshotToIngestionTreeRoot({
        status: 'ready',
        providers: [
            {
                name: 'file-share',
                displayName: 'File Share',
                description: 'Ingests content sourced from File Share.'
            }
        ],
        providerNodes: []
    });

    assert.equal(root.id, 'search-studio.ingestion.root');
    assert.equal(root.visible, false);
    assert.equal(root.children.length, 1);
    assert.deepEqual(
        root.children[0].children.map(child => ({ id: child.id, kind: child.kind, iconClass: child.iconClass })),
        [
            {
                id: 'ingestion:file-share:by-id',
                kind: 'by-id',
                iconClass: 'codicon codicon-symbol-numeric'
            },
            {
                id: 'ingestion:file-share:all-unindexed',
                kind: 'all-unindexed',
                iconClass: 'codicon codicon-layers'
            },
            {
                id: 'ingestion:file-share:by-context',
                kind: 'by-context',
                iconClass: 'codicon codicon-symbol-key'
            }
        ]);
});

test('mapProviderCatalogSnapshotToIngestionTreeRoot produces clear status nodes for loading, empty, and error states', () => {
    const loadingRoot = mapProviderCatalogSnapshotToIngestionTreeRoot({
        status: 'loading',
        providers: [],
        providerNodes: []
    });
    const emptyRoot = mapProviderCatalogSnapshotToIngestionTreeRoot({
        status: 'ready',
        providers: [],
        providerNodes: []
    });
    const errorRoot = mapProviderCatalogSnapshotToIngestionTreeRoot({
        status: 'error',
        providers: [],
        providerNodes: [],
        errorMessage: 'StudioApiHost /providers is unavailable.'
    });

    assert.deepEqual(
        [loadingRoot, emptyRoot, errorRoot].map(root => ({
            kind: root.children[0].kind,
            label: root.children[0].label
        })),
        [
            {
                kind: 'status',
                label: 'Loading Studio providers...'
            },
            {
                kind: 'status',
                label: 'No providers were returned by StudioApiHost.'
            },
            {
                kind: 'status',
                label: 'StudioApiHost /providers is unavailable.'
            }
        ]);
});

test('resolveIngestionTreeNodeOpenTarget keeps ingestion overview and mode routing assumptions stable', () => {
    assert.equal(resolveIngestionTreeNodeOpenTarget({ kind: 'provider-root' }), 'ingestion-overview');
    assert.equal(resolveIngestionTreeNodeOpenTarget({ kind: 'by-id' }), 'ingestion-by-id');
    assert.equal(resolveIngestionTreeNodeOpenTarget({ kind: 'all-unindexed' }), 'ingestion-all-unindexed');
    assert.equal(resolveIngestionTreeNodeOpenTarget({ kind: 'by-context' }), 'ingestion-by-context');
    assert.equal(resolveIngestionTreeNodeOpenTarget({ kind: 'status' }), undefined);
});
