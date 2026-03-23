import type {
    SearchStudioApiIngestionAcceptedOperationResponse,
    SearchStudioApiIngestionOperationConflictResponse,
    SearchStudioApiIngestionOperationEventResponse,
    SearchStudioApiIngestionOperationStateResponse
} from '../api/search-studio-api-types';

export interface SearchStudioIngestionOperationViewState {
    readonly selectedProviderName?: string;
    readonly activeOperation?: SearchStudioApiIngestionOperationStateResponse;
    readonly isRecovering: boolean;
    readonly isStarting: boolean;
    readonly isResetting: boolean;
}

export function canStartAllUnindexedOperation(state: SearchStudioIngestionOperationViewState): boolean {
    return Boolean(state.selectedProviderName)
        && !hasActiveOperation(state.activeOperation)
        && !state.isRecovering
        && !state.isStarting
        && !state.isResetting;
}

export function canResetAllUnindexedOperation(state: SearchStudioIngestionOperationViewState): boolean {
    return canStartAllUnindexedOperation(state);
}

export function hasActiveOperation(operation?: SearchStudioApiIngestionOperationStateResponse): boolean {
    return operation?.status === 'queued' || operation?.status === 'running';
}

export function hasCompletedOperation(operation?: SearchStudioApiIngestionOperationStateResponse): boolean {
    return operation?.status === 'succeeded' || operation?.status === 'failed';
}

export function mapAcceptedOperationToState(
    acceptedOperation: SearchStudioApiIngestionAcceptedOperationResponse,
    message: string
): SearchStudioApiIngestionOperationStateResponse
{
    return {
        operationId: acceptedOperation.operationId,
        provider: acceptedOperation.provider,
        operationType: acceptedOperation.operationType,
        context: acceptedOperation.context,
        status: acceptedOperation.status,
        message,
        completed: null,
        total: null,
        startedUtc: new Date().toISOString(),
        completedUtc: null,
        failureCode: null
    };
}

export function applyOperationEvent(
    operation: SearchStudioApiIngestionOperationStateResponse,
    event: SearchStudioApiIngestionOperationEventResponse
): SearchStudioApiIngestionOperationStateResponse
{
    return {
        ...operation,
        status: event.status,
        message: event.message,
        completed: event.completed,
        total: event.total,
        completedUtc: event.status === 'succeeded' || event.status === 'failed'
            ? event.timestampUtc
            : operation.completedUtc,
        failureCode: event.failureCode
    };
}

export function mapOperationConflictMessage(
    conflict: SearchStudioApiIngestionOperationConflictResponse | undefined
): string
{
    if (!conflict) {
        return 'Another ingestion operation is already active.';
    }

    return `${conflict.message} Active operation: ${conflict.activeOperationType} for ${conflict.activeProvider}.`;
}

export function formatOperationProgress(operation?: SearchStudioApiIngestionOperationStateResponse): string {
    if (!operation) {
        return 'No operation in progress.';
    }

    if (operation.completed === null || operation.completed === undefined || operation.total === null || operation.total === undefined) {
        return operation.message;
    }

    return `${operation.message} (${operation.completed} / ${operation.total})`;
}
