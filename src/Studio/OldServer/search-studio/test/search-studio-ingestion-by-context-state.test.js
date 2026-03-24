const test = require('node:test');
const assert = require('node:assert/strict');
const {
    canLoadContexts,
    canResetContextOperation,
    canStartContextOperation,
    getSelectedContextDisplayName
} = require('../lib/browser/ingestion/search-studio-ingestion-by-context-state.js');

test('context actions stay disabled until a provider is selected, contexts load, and a context is chosen', () => {
    const contexts = [
        { value: '12', displayName: 'Admiralty', isDefault: false }
    ];

    assert.equal(canLoadContexts({ selectedProviderName: undefined, contexts: [], selectedContextValue: undefined, activeOperation: undefined, isLoadingContexts: false, isRecovering: false, isStarting: false, isResetting: false }), false);
    assert.equal(canLoadContexts({ selectedProviderName: 'file-share', contexts: [], selectedContextValue: undefined, activeOperation: undefined, isLoadingContexts: false, isRecovering: false, isStarting: false, isResetting: false }), true);
    assert.equal(canStartContextOperation({ selectedProviderName: 'file-share', contexts, selectedContextValue: undefined, activeOperation: undefined, isLoadingContexts: false, isRecovering: false, isStarting: false, isResetting: false }), false);
    assert.equal(canStartContextOperation({ selectedProviderName: 'file-share', contexts, selectedContextValue: '12', activeOperation: undefined, isLoadingContexts: false, isRecovering: false, isStarting: false, isResetting: false }), true);
    assert.equal(canResetContextOperation({ selectedProviderName: 'file-share', contexts, selectedContextValue: '12', activeOperation: { status: 'running' }, isLoadingContexts: false, isRecovering: false, isStarting: false, isResetting: false }), false);
});

test('getSelectedContextDisplayName keeps the selected opaque context value separate from the UI label', () => {
    const contexts = [
        { value: '7', displayName: 'AVCS', isDefault: false },
        { value: '12', displayName: 'Admiralty', isDefault: false }
    ];

    assert.equal(getSelectedContextDisplayName(contexts, '12'), 'Admiralty');
    assert.equal(getSelectedContextDisplayName(contexts, '999'), undefined);
});
