import type {
    SearchStudioApiIngestionPayloadEnvelope,
    SearchStudioApiIngestionOperationStateResponse,
    SearchStudioApiIngestionSubmitPayloadRequest
} from '../api/search-studio-api-types';
import { hasActiveOperation } from './search-studio-ingestion-operation-state';

export interface SearchStudioIngestionByIdViewState {
    readonly selectedProviderName?: string;
    readonly identifier: string;
    readonly fetchedPayload?: SearchStudioApiIngestionPayloadEnvelope;
    readonly activeOperation?: SearchStudioApiIngestionOperationStateResponse;
    readonly isRecoveringOperation: boolean;
    readonly isFetching: boolean;
    readonly isSubmitting: boolean;
}

export function canFetchById(state: SearchStudioIngestionByIdViewState): boolean {
    return Boolean(state.selectedProviderName)
        && state.identifier.trim().length > 0
        && !state.isFetching
        && !state.isSubmitting;
}

export function canSubmitFetchedPayload(state: SearchStudioIngestionByIdViewState): boolean {
    return Boolean(state.selectedProviderName)
        && Boolean(state.fetchedPayload)
        && !hasActiveOperation(state.activeOperation)
        && !state.isRecoveringOperation
        && !state.isFetching
        && !state.isSubmitting;
}

export function createSubmitPayloadRequest(
    fetchedPayload?: SearchStudioApiIngestionPayloadEnvelope
): SearchStudioApiIngestionSubmitPayloadRequest | undefined
{
    if (!fetchedPayload) {
        return undefined;
    }

    return {
        id: fetchedPayload.id,
        payload: fetchedPayload.payload
    };
}

export function getPayloadPreviewText(fetchedPayload?: SearchStudioApiIngestionPayloadEnvelope): string {
    return fetchedPayload
        ? JSON.stringify(fetchedPayload.payload, undefined, 2)
        : '';
}

export function mapByIdActionErrorMessage(
    action: 'fetch' | 'submit',
    identifier: string,
    error: { status?: number } | undefined
): string
{
    const status = error?.status;

    if (action === 'fetch') {
        switch (status) {
            case 400:
                return 'Select a provider and enter a valid id before fetching.';
            case 404:
                return `No payload was found for id '${identifier}'.`;
            default:
                return `Failed to fetch payload for id '${identifier}'.`;
        }
    }

    if (status === 400) {
        return 'The fetched payload is no longer valid for submission.';
    }

    if (status === 409) {
        return 'Another ingestion operation is already active.';
    }

    return `Failed to submit payload for id '${identifier}'.`;
}
