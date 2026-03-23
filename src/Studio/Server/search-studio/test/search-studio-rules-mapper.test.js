const test = require('node:test');
const assert = require('node:assert/strict');
const {
    mapRuleDiscoveryResponseToRulesSnapshot,
    mapRulesCatalogSnapshotToRulesTreeRoot,
    resolveRulesTreeNodeOpenTarget
} = require('../lib/browser/rules/search-studio-rules-mapper.js');

test('mapRuleDiscoveryResponseToRulesSnapshot groups rules beneath provider roots with checker and rules nodes', () => {
    const snapshot = mapRuleDiscoveryResponseToRulesSnapshot({
        schemaVersion: '1.0',
        providers: [
            {
                providerName: 'file-share',
                displayName: 'File Share',
                description: 'Ingests content sourced from File Share.',
                rules: [
                    {
                        id: 'rule-1',
                        title: 'Rule 1',
                        context: 'adds-s100',
                        description: 'First rule.',
                        enabled: true
                    },
                    {
                        id: 'rule-2',
                        title: 'Rule 2',
                        enabled: false
                    }
                ]
            }
        ]
    });

    assert.equal(snapshot.status, 'ready');
    assert.equal(snapshot.providers.length, 1);
    assert.equal(snapshot.providers[0].rootNode.children.length, 2);
    assert.equal(snapshot.providers[0].rootNode.children[0].kind, 'rule-checker');
    assert.equal(snapshot.providers[0].rootNode.children[1].kind, 'rules-group');
    assert.deepEqual(
        snapshot.providers[0].ruleNodes.map(node => ({ id: node.ruleId, badge: node.badge.value })),
        [
            { id: 'rule-1', badge: 'ACTIVE' },
            { id: 'rule-2', badge: 'DISABLED' }
        ]);
});

test('mapRulesCatalogSnapshotToRulesTreeRoot maps provider, checker, grouping, and rule nodes into a native tree shape', () => {
    const snapshot = mapRuleDiscoveryResponseToRulesSnapshot({
        schemaVersion: '1.0',
        providers: [
            {
                providerName: 'file-share',
                displayName: 'File Share',
                description: 'Ingests content sourced from File Share.',
                rules: [
                    {
                        id: 'rule-1',
                        title: 'Rule 1',
                        context: 'adds-s100',
                        enabled: true
                    }
                ]
            }
        ]
    });

    const root = mapRulesCatalogSnapshotToRulesTreeRoot(snapshot);

    assert.equal(root.id, 'search-studio.rules.root');
    assert.equal(root.visible, false);
    assert.equal(root.children.length, 1);
    assert.deepEqual(
        root.children[0].children.map(child => ({ id: child.id, kind: child.kind, iconClass: child.iconClass })),
        [
            {
                id: 'rules:file-share:checker',
                kind: 'rule-checker',
                iconClass: 'codicon codicon-symbol-event'
            },
            {
                id: 'rules:file-share:group',
                kind: 'rules-group',
                iconClass: 'codicon codicon-folder-library'
            }
        ]);
    assert.equal(root.children[0].children[1].children[0].id, 'rules:file-share:rule:rule-1');
});

test('mapRulesCatalogSnapshotToRulesTreeRoot produces clear status nodes for loading, empty, and error states', () => {
    const loadingRoot = mapRulesCatalogSnapshotToRulesTreeRoot({
        status: 'loading',
        providers: []
    });
    const emptyRoot = mapRulesCatalogSnapshotToRulesTreeRoot({
        status: 'ready',
        providers: []
    });
    const errorRoot = mapRulesCatalogSnapshotToRulesTreeRoot({
        status: 'error',
        providers: [],
        errorMessage: 'StudioApiHost /rules is unavailable.'
    });

    assert.deepEqual(
        [loadingRoot, emptyRoot, errorRoot].map(root => ({
            kind: root.children[0].kind,
            label: root.children[0].label
        })),
        [
            {
                kind: 'status',
                label: 'Loading Studio rules...'
            },
            {
                kind: 'status',
                label: 'No rules were returned by StudioApiHost.'
            },
            {
                kind: 'status',
                label: 'StudioApiHost /rules is unavailable.'
            }
        ]);
});

test('resolveRulesTreeNodeOpenTarget keeps rules overview, checker, and rule editor routing assumptions stable', () => {
    assert.equal(resolveRulesTreeNodeOpenTarget({ kind: 'provider-root' }), 'rules-overview');
    assert.equal(resolveRulesTreeNodeOpenTarget({ kind: 'rules-group' }), 'rules-overview');
    assert.equal(resolveRulesTreeNodeOpenTarget({ kind: 'rule-checker' }), 'rule-checker');
    assert.equal(resolveRulesTreeNodeOpenTarget({ kind: 'rule' }), 'rule-editor');
    assert.equal(resolveRulesTreeNodeOpenTarget({ kind: 'status' }), undefined);
});

test('mapRuleDiscoveryResponseToRulesSnapshot derives provider summaries and placeholder invalid counts', () => {
    const snapshot = mapRuleDiscoveryResponseToRulesSnapshot({
        schemaVersion: '1.0',
        providers: [
            {
                providerName: 'file-share',
                displayName: 'File Share',
                rules: [
                    {
                        id: 'rule-1',
                        title: 'Rule 1',
                        enabled: true
                    }
                ]
            },
            {
                providerName: 'other-provider',
                displayName: 'Other Provider',
                rules: []
            }
        ]
    });

    assert.deepEqual(
        snapshot.providers.map(group => ({
            provider: group.provider.name,
            total: group.summary.totalRuleCount,
            active: group.summary.activeRuleCount,
            invalid: group.summary.invalidRuleCount,
            invalidPlaceholder: group.summary.invalidRuleCountIsPlaceholder
        })),
        [
            { provider: 'file-share', total: 1, active: 1, invalid: 0, invalidPlaceholder: true },
            { provider: 'other-provider', total: 0, active: 0, invalid: 0, invalidPlaceholder: true }
        ]);
});
