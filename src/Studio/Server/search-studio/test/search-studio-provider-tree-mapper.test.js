const test = require('node:test');
const assert = require('node:assert/strict');
const { mapProviderDescriptorsToProviderTreeNodes } = require('../lib/browser/providers/search-studio-provider-tree-mapper.js');

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
