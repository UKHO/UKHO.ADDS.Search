const test = require('node:test');
const assert = require('node:assert/strict');
const {
    mapProviderCatalogSnapshotToProvidersTreeRoot,
    mapProviderDescriptorsToProviderTreeNodes
} = require('../lib/browser/providers/search-studio-provider-tree-mapper.js');
const {
    mapRuleDiscoveryResponseToRulesSnapshot,
    mapRulesCatalogSnapshotToRulesTreeRoot
} = require('../lib/browser/rules/search-studio-rules-mapper.js');
const {
    mapProviderCatalogSnapshotToIngestionTreeRoot
} = require('../lib/browser/ingestion/search-studio-ingestion-mapper.js');
const {
    rememberTopLevelExpansionState,
    synchronizeTopLevelExpansionState
} = require('../lib/browser/common/search-studio-top-level-expansion-state.js');

function createProviderSnapshot() {
    const providers = [
        {
            name: 'file-share',
            displayName: 'File Share',
            description: 'Ingests content sourced from File Share.'
        },
        {
            name: 'admiralty',
            displayName: 'Admiralty',
            description: 'Ingests content sourced from Admiralty.'
        }
    ];

    return {
        status: 'ready',
        providers,
        providerNodes: mapProviderDescriptorsToProviderTreeNodes(providers)
    };
}

function createRulesRoot() {
    return mapRulesCatalogSnapshotToRulesTreeRoot(mapRuleDiscoveryResponseToRulesSnapshot({
        schemaVersion: '1.0',
        providers: [
            {
                providerName: 'file-share',
                displayName: 'File Share',
                description: 'Ingests content sourced from File Share.',
                rules: []
            },
            {
                providerName: 'admiralty',
                displayName: 'Admiralty',
                description: 'Ingests content sourced from Admiralty.',
                rules: []
            }
        ]
    }));
}

function createIngestionRoot() {
    return mapProviderCatalogSnapshotToIngestionTreeRoot(createProviderSnapshot());
}

test('synchronizeTopLevelExpansionState expands only the first top-level node by default', () => {
    const root = mapProviderCatalogSnapshotToProvidersTreeRoot(createProviderSnapshot());
    const expansionState = new Map();

    synchronizeTopLevelExpansionState(root, expansionState);

    assert.equal(root.children[0].expanded, true);
    assert.equal(root.children[1].expanded, false);
    assert.deepEqual(
        [...expansionState.entries()],
        [
            ['provider:file-share', true],
            ['provider:admiralty', false]
        ]);
});

test('rememberTopLevelExpansionState and synchronizeTopLevelExpansionState preserve later user-driven top-level expansion choices', () => {
    const initialRoot = mapProviderCatalogSnapshotToProvidersTreeRoot(createProviderSnapshot());
    const expansionState = new Map();

    synchronizeTopLevelExpansionState(initialRoot, expansionState);

    initialRoot.children[0].expanded = false;
    initialRoot.children[1].expanded = true;

    assert.equal(rememberTopLevelExpansionState(initialRoot, initialRoot.children[0], expansionState), true);
    assert.equal(rememberTopLevelExpansionState(initialRoot, initialRoot.children[1], expansionState), true);

    const rebuiltRoot = mapProviderCatalogSnapshotToProvidersTreeRoot(createProviderSnapshot());

    synchronizeTopLevelExpansionState(rebuiltRoot, expansionState);

    assert.equal(rebuiltRoot.children[0].expanded, false);
    assert.equal(rebuiltRoot.children[1].expanded, true);
});

test('rememberTopLevelExpansionState ignores non-top-level expansion events', () => {
    const root = mapProviderCatalogSnapshotToProvidersTreeRoot(createProviderSnapshot());
    const expansionState = new Map();

    synchronizeTopLevelExpansionState(root, expansionState);
    root.children[0].children[0].expanded = true;

    assert.equal(rememberTopLevelExpansionState(root, root.children[0].children[0], expansionState), false);
    assert.deepEqual(
        [...expansionState.entries()],
        [
            ['provider:file-share', true],
            ['provider:admiralty', false]
        ]);
});

test('synchronizeTopLevelExpansionState is a no-op for status-only trees', () => {
    const root = mapProviderCatalogSnapshotToProvidersTreeRoot({
        status: 'loading',
        providers: [],
        providerNodes: []
    });
    const expansionState = new Map([['provider:file-share', true]]);

    synchronizeTopLevelExpansionState(root, expansionState);

    assert.equal(root.children[0].kind, 'status');
    assert.equal('expanded' in root.children[0], false);
    assert.equal(expansionState.size, 0);
});

test('synchronizeTopLevelExpansionState expands only the first top-level root in rules trees', () => {
    const root = createRulesRoot();
    const expansionState = new Map();

    synchronizeTopLevelExpansionState(root, expansionState);

    assert.equal(root.children[0].id, 'rules:file-share');
    assert.equal(root.children[0].expanded, true);
    assert.equal(root.children[1].expanded, false);
});

test('synchronizeTopLevelExpansionState expands only the first top-level root in ingestion trees', () => {
    const root = createIngestionRoot();
    const expansionState = new Map();

    synchronizeTopLevelExpansionState(root, expansionState);

    assert.equal(root.children[0].id, 'ingestion:file-share');
    assert.equal(root.children[0].expanded, true);
    assert.equal(root.children[1].expanded, false);
});
