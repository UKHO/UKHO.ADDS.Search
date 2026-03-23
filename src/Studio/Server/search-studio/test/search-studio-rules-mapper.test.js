const test = require('node:test');
const assert = require('node:assert/strict');
const { mapRuleDiscoveryResponseToRulesSnapshot } = require('../lib/browser/rules/search-studio-rules-mapper.js');

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
