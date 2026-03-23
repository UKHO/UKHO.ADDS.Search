const test = require('node:test');
const assert = require('node:assert/strict');
const {
    applyOperationEvent,
    canResetAllUnindexedOperation,
    canStartAllUnindexedOperation,
    formatOperationProgress,
    mapAcceptedOperationToState,
    mapOperationConflictMessage
} = require('../lib/browser/ingestion/search-studio-ingestion-operation-state.js');

test('all-unindexed actions require a selected provider and no active operation', () => {
    assert.equal(canStartAllUnindexedOperation({ selectedProviderName: undefined, activeOperation: undefined, isRecovering: false, isStarting: false, isResetting: false }), false);
    assert.equal(canStartAllUnindexedOperation({ selectedProviderName: 'file-share', activeOperation: { status: 'running' }, isRecovering: false, isStarting: false, isResetting: false }), false);
    assert.equal(canStartAllUnindexedOperation({ selectedProviderName: 'file-share', activeOperation: undefined, isRecovering: false, isStarting: false, isResetting: false }), true);
    assert.equal(canResetAllUnindexedOperation({ selectedProviderName: 'file-share', activeOperation: undefined, isRecovering: false, isStarting: false, isResetting: false }), true);
});

test('accepted operations and live events update progress and terminal state consistently', () => {
    const accepted = {
        operationId: '4a53becc-c436-45d2-a6b3-4030b19ca5b7',
        provider: 'file-share',
        operationType: 'index-all',
        context: null,
        status: 'queued'
    };

    const queued = mapAcceptedOperationToState(accepted, 'Queued operation.');
    const running = applyOperationEvent(queued, {
        eventType: 'progress',
        operationId: accepted.operationId,
        status: 'running',
        message: 'Processed 2 of 5.',
        completed: 2,
        total: 5,
        timestampUtc: '2026-01-01T10:05:00Z',
        failureCode: null
    });
    const completed = applyOperationEvent(running, {
        eventType: 'progress',
        operationId: accepted.operationId,
        status: 'succeeded',
        message: 'Processed 5 of 5.',
        completed: 5,
        total: 5,
        timestampUtc: '2026-01-01T10:06:00Z',
        failureCode: null
    });

    assert.equal(running.status, 'running');
    assert.equal(formatOperationProgress(running), 'Processed 2 of 5. (2 / 5)');
    assert.equal(completed.status, 'succeeded');
    assert.equal(completed.completedUtc, '2026-01-01T10:06:00Z');
    assert.equal(formatOperationProgress(completed), 'Processed 5 of 5. (5 / 5)');
});

test('conflict messages remain provider-neutral while surfacing active operation details', () => {
    assert.equal(mapOperationConflictMessage(undefined), 'Another ingestion operation is already active.');
    assert.equal(
        mapOperationConflictMessage({
            message: 'Another ingestion operation is already active.',
            activeOperationId: '4a53becc-c436-45d2-a6b3-4030b19ca5b7',
            activeProvider: 'file-share',
            activeOperationType: 'index-all'
        }),
        'Another ingestion operation is already active. Active operation: index-all for file-share.');
});
