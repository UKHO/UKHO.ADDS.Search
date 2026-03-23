const test = require('node:test');
const assert = require('node:assert/strict');
const {
    canFetchById,
    canSubmitFetchedPayload,
    createSubmitPayloadRequest,
    mapByIdActionErrorMessage
} = require('../lib/browser/ingestion/search-studio-ingestion-by-id-state.js');

test('canFetchById requires a selected provider and a non-empty id before enabling fetch', () => {
    assert.equal(canFetchById({ selectedProviderName: undefined, identifier: 'batch-123', isFetching: false, isSubmitting: false }), false);
    assert.equal(canFetchById({ selectedProviderName: 'file-share', identifier: '   ', isFetching: false, isSubmitting: false }), false);
    assert.equal(canFetchById({ selectedProviderName: 'file-share', identifier: 'batch-123', isFetching: false, isSubmitting: false }), true);
});

test('createSubmitPayloadRequest reuses the fetched payload unchanged and enables submit only after fetch', () => {
    const payload = { RequestType: 'IndexItem', IndexItem: { Id: 'batch-123' } };
    const fetchedPayload = {
        id: 'batch-123',
        payload
    };

    const request = createSubmitPayloadRequest(fetchedPayload);

    assert.equal(canSubmitFetchedPayload({ selectedProviderName: 'file-share', identifier: 'batch-123', fetchedPayload, activeOperation: undefined, isRecoveringOperation: false, isFetching: false, isSubmitting: false }), true);
    assert.equal(request.id, 'batch-123');
    assert.equal(request.payload, payload);
});

test('by-id fetch stays available during an active operation while submit is blocked until the operation completes', () => {
    const fetchedPayload = {
        id: 'batch-123',
        payload: { RequestType: 'IndexItem' }
    };

    assert.equal(
        canFetchById({
            selectedProviderName: 'file-share',
            identifier: 'batch-123',
            activeOperation: { status: 'running' },
            isRecoveringOperation: false,
            isFetching: false,
            isSubmitting: false
        }),
        true);

    assert.equal(
        canSubmitFetchedPayload({
            selectedProviderName: 'file-share',
            identifier: 'batch-123',
            fetchedPayload,
            activeOperation: { status: 'running' },
            isRecoveringOperation: false,
            isFetching: false,
            isSubmitting: false
        }),
        false);
});

test('mapByIdActionErrorMessage keeps not-found and submit-failure feedback stable', () => {
    assert.equal(mapByIdActionErrorMessage('fetch', 'missing-id', { status: 404 }), "No payload was found for id 'missing-id'.");
    assert.equal(mapByIdActionErrorMessage('submit', 'batch-123', { status: 409 }), 'Another ingestion operation is already active.');
    assert.equal(mapByIdActionErrorMessage('submit', 'batch-123', { status: 500 }), "Failed to submit payload for id 'batch-123'.");
});
