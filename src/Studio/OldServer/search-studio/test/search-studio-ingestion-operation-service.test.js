const test = require('node:test');
const assert = require('node:assert/strict');
const { SearchStudioIngestionOperationService } = require('../lib/browser/ingestion/search-studio-ingestion-operation-service.js');

class FakeEventSource {
    static instances = [];

    constructor(url) {
        this.url = url;
        this.closed = false;
        this.onmessage = undefined;
        this.onerror = undefined;
        FakeEventSource.instances.push(this);
    }

    close() {
        this.closed = true;
    }

    emitMessage(payload) {
        this.onmessage?.({ data: JSON.stringify(payload) });
    }

    emitError() {
        this.onerror?.();
    }

    static reset() {
        FakeEventSource.instances = [];
    }
}

function waitForAsyncWork() {
    return new Promise(resolve => setTimeout(resolve, 0));
}

test('operation service reads back the final operation state after a terminal SSE event', async () => {
    const previousEventSource = global.EventSource;
    FakeEventSource.reset();
    global.EventSource = FakeEventSource;

    const acceptedOperation = {
        operationId: '4a53becc-c436-45d2-a6b3-4030b19ca5b7',
        provider: 'file-share',
        operationType: 'index-all',
        context: null,
        status: 'queued'
    };
    const finalOperation = {
        operationId: acceptedOperation.operationId,
        provider: 'file-share',
        operationType: 'index-all',
        context: null,
        status: 'succeeded',
        message: 'Processed 5 of 5.',
        completed: 5,
        total: 5,
        startedUtc: '2026-01-01T10:00:00Z',
        completedUtc: '2026-01-01T10:06:00Z',
        failureCode: null
    };
    const updates = [];
    let getOperationCalls = 0;

    try {
        const service = new SearchStudioIngestionOperationService();
        service._apiClient = {
            startIngestionAllUnindexed: async () => acceptedOperation,
            getOperationEventsUrl: async operationId => `https://studio.example/operations/${operationId}/events`,
            getOperation: async () => {
                getOperationCalls += 1;
                return finalOperation;
            }
        };

        await service.startAllUnindexedOperation('file-share', operation => updates.push(operation));

        assert.equal(FakeEventSource.instances.length, 1);
        FakeEventSource.instances[0].emitMessage({
            eventType: 'progress',
            operationId: acceptedOperation.operationId,
            status: 'succeeded',
            message: 'Processed 5 of 5.',
            completed: 5,
            total: 5,
            timestampUtc: '2026-01-01T10:06:00Z',
            failureCode: null
        });

        await waitForAsyncWork();

        assert.equal(getOperationCalls, 1);
        assert.equal(updates.at(-1).status, 'succeeded');
        assert.equal(updates.at(-1).completedUtc, '2026-01-01T10:06:00Z');
        assert.equal(FakeEventSource.instances[0].closed, true);
    } finally {
        global.EventSource = previousEventSource;
    }
});
