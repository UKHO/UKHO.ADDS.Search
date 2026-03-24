const test = require('node:test');
const assert = require('node:assert/strict');
const { resolvePreferredProvider } = require('../lib/browser/common/search-studio-provider-resolution.js');

const providers = [
    { name: 'file-share', displayName: 'File Share' },
    { name: 'other-provider', displayName: 'Other Provider' }
];

test('resolvePreferredProvider prefers an explicit provider name when it exists', () => {
    const provider = resolvePreferredProvider(providers, 'file-share', 'other-provider');

    assert.equal(provider.name, 'other-provider');
});

test('resolvePreferredProvider falls back to the selected provider and then the first provider', () => {
    const selectedProvider = resolvePreferredProvider(providers, 'file-share');
    const fallbackProvider = resolvePreferredProvider(providers);

    assert.equal(selectedProvider.name, 'file-share');
    assert.equal(fallbackProvider.name, 'file-share');
});
