const test = require('node:test');
const assert = require('node:assert/strict');
const {
    mapProviderCatalogSnapshotToProvidersTreeRoot,
    mapProviderDescriptorsToProviderTreeNodes,
    resolveProviderTreeNodeOpenTarget
} = require('../lib/browser/providers/search-studio-provider-tree-mapper.js');

test('mapProviderDescriptorsToProviderTreeNodes creates provider roots with queue and dead-letter children', () => {
    const nodes = mapProviderDescriptorsToProviderTreeNodes([
        {
            name: 'file-share',
            displayName: 'File Share',
            description: 'Ingests content sourced from File Share.'
        }
    ]);

    assert.equal(nodes.length, 1);
    assert.equal(nodes[0].id, 'provider:file-share');
    assert.equal(nodes[0].kind, 'provider-root');
    assert.equal(nodes[0].badge.value, 'API');
    assert.deepEqual(
        nodes[0].children.map(child => ({ id: child.id, kind: child.kind, label: child.label })),
        [
            {
                id: 'provider:file-share:queue',
                kind: 'queue',
                label: 'Queue'
            },
            {
                id: 'provider:file-share:dead-letters',
                kind: 'dead-letters',
                label: 'Dead letters'
            }
        ]);
});

test('mapProviderDescriptorsToProviderTreeNodes keeps provider metadata on each child node', () => {
    const [node] = mapProviderDescriptorsToProviderTreeNodes([
        {
            name: 'file-share',
            displayName: 'File Share',
            description: 'Ingests content sourced from File Share.'
        }
    ]);

    assert.equal(node.provider.name, 'file-share');
    assert.equal(node.children[0].provider.displayName, 'File Share');
    assert.equal(node.children[1].provider.description, 'Ingests content sourced from File Share.');
});

test('mapProviderCatalogSnapshotToProvidersTreeRoot creates stable provider and child tree nodes for the native tree widget', () => {
    const root = mapProviderCatalogSnapshotToProvidersTreeRoot({
        status: 'ready',
        providers: [
            {
                name: 'file-share',
                displayName: 'File Share',
                description: 'Ingests content sourced from File Share.'
            }
        ],
        providerNodes: mapProviderDescriptorsToProviderTreeNodes([
            {
                name: 'file-share',
                displayName: 'File Share',
                description: 'Ingests content sourced from File Share.'
            }
        ])
    });

    assert.equal(root.id, 'search-studio.providers.root');
    assert.equal(root.visible, false);
    assert.equal(root.children.length, 1);
    assert.equal(root.children[0].id, 'provider:file-share');
    assert.equal(root.children[0].iconClass, 'codicon codicon-database');
    assert.deepEqual(
        root.children[0].children.map(child => ({ id: child.id, iconClass: child.iconClass })),
        [
            {
                id: 'provider:file-share:queue',
                iconClass: 'codicon codicon-list-unordered'
            },
            {
                id: 'provider:file-share:dead-letters',
                iconClass: 'codicon codicon-archive'
            }
        ]);
});

test('mapProviderCatalogSnapshotToProvidersTreeRoot produces clear status nodes for loading, empty, and error states', () => {
    const loadingRoot = mapProviderCatalogSnapshotToProvidersTreeRoot({
        status: 'loading',
        providers: [],
        providerNodes: []
    });
    const emptyRoot = mapProviderCatalogSnapshotToProvidersTreeRoot({
        status: 'ready',
        providers: [],
        providerNodes: []
    });
    const errorRoot = mapProviderCatalogSnapshotToProvidersTreeRoot({
        status: 'error',
        providers: [],
        providerNodes: [],
        errorMessage: 'StudioApiHost is unavailable.'
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
                label: 'StudioApiHost is unavailable.'
            }
        ]);
});

test('resolveProviderTreeNodeOpenTarget keeps provider overview and child routing assumptions stable', () => {
    assert.equal(resolveProviderTreeNodeOpenTarget({ kind: 'provider-root' }), 'provider-overview');
    assert.equal(resolveProviderTreeNodeOpenTarget({ kind: 'queue' }), 'provider-queue');
    assert.equal(resolveProviderTreeNodeOpenTarget({ kind: 'dead-letters' }), 'provider-dead-letters');
    assert.equal(resolveProviderTreeNodeOpenTarget({ kind: 'status' }), undefined);
});
