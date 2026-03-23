import { inject, injectable } from '@theia/core/shared/inversify';
import type {
    SearchStudioApiIngestionAcceptedOperationResponse,
    SearchStudioApiIngestionOperationEventResponse,
    SearchStudioApiIngestionOperationStateResponse
} from '../api/search-studio-api-types';
import { SearchStudioApiClient } from '../api/search-studio-api-client';
import {
    applyOperationEvent,
    hasActiveOperation,
    hasCompletedOperation,
    mapAcceptedOperationToState
} from './search-studio-ingestion-operation-state';

@injectable()
export class SearchStudioIngestionOperationService {

    @inject(SearchStudioApiClient)
    protected readonly _apiClient!: SearchStudioApiClient;

    protected _eventSource?: EventSource;

    disposeSubscription(): void {
        this._eventSource?.close();
        this._eventSource = undefined;
    }

    async recoverActiveOperation(
        onOperationUpdated: (operation: SearchStudioApiIngestionOperationStateResponse) => void
    ): Promise<SearchStudioApiIngestionOperationStateResponse | undefined>
    {
        const activeOperation = await this._apiClient.getActiveOperation();
        if (!activeOperation) {
            this.disposeSubscription();
            return undefined;
        }

        if (hasActiveOperation(activeOperation)) {
            await this.subscribe(activeOperation.operationId, onOperationUpdated, activeOperation);
        }

        return activeOperation;
    }

    async startAllUnindexedOperation(
        providerName: string,
        onOperationUpdated: (operation: SearchStudioApiIngestionOperationStateResponse) => void
    ): Promise<SearchStudioApiIngestionOperationStateResponse>
    {
        const acceptedOperation = await this._apiClient.startIngestionAllUnindexed(providerName);
        return this.subscribeAcceptedOperation(acceptedOperation, 'Queued operation.', onOperationUpdated);
    }

    async startContextOperation(
        providerName: string,
        context: string,
        onOperationUpdated: (operation: SearchStudioApiIngestionOperationStateResponse) => void
    ): Promise<SearchStudioApiIngestionOperationStateResponse>
    {
        const acceptedOperation = await this._apiClient.startIngestionContext(providerName, context);
        return this.subscribeAcceptedOperation(acceptedOperation, 'Queued context operation.', onOperationUpdated);
    }

    async resetIndexingStatusOperation(
        providerName: string,
        onOperationUpdated: (operation: SearchStudioApiIngestionOperationStateResponse) => void
    ): Promise<SearchStudioApiIngestionOperationStateResponse>
    {
        const acceptedOperation = await this._apiClient.resetIngestionIndexingStatus(providerName);
        return this.subscribeAcceptedOperation(acceptedOperation, 'Queued reset operation.', onOperationUpdated);
    }

    async resetIndexingStatusForContextOperation(
        providerName: string,
        context: string,
        onOperationUpdated: (operation: SearchStudioApiIngestionOperationStateResponse) => void
    ): Promise<SearchStudioApiIngestionOperationStateResponse>
    {
        const acceptedOperation = await this._apiClient.resetIngestionIndexingStatusForContext(providerName, context);
        return this.subscribeAcceptedOperation(acceptedOperation, 'Queued context reset operation.', onOperationUpdated);
    }

    protected async subscribeAcceptedOperation(
        acceptedOperation: SearchStudioApiIngestionAcceptedOperationResponse,
        queuedMessage: string,
        onOperationUpdated: (operation: SearchStudioApiIngestionOperationStateResponse) => void
    ): Promise<SearchStudioApiIngestionOperationStateResponse>
    {
        const operation = mapAcceptedOperationToState(acceptedOperation, queuedMessage);
        await this.subscribe(operation.operationId, onOperationUpdated, operation);
        return operation;
    }

    protected async subscribe(
        operationId: string,
        onOperationUpdated: (operation: SearchStudioApiIngestionOperationStateResponse) => void,
        initialState: SearchStudioApiIngestionOperationStateResponse
    ): Promise<void>
    {
        this.disposeSubscription();

        let currentState = initialState;
        const eventsUrl = await this._apiClient.getOperationEventsUrl(operationId);
        let hasRequestedFinalReadback = false;

        this._eventSource = new EventSource(eventsUrl);
        this._eventSource.onmessage = event => {
            const operationEvent = JSON.parse(event.data) as SearchStudioApiIngestionOperationEventResponse;
            currentState = applyOperationEvent(currentState, operationEvent);
            onOperationUpdated(currentState);

            if (hasCompletedOperation(currentState) && !hasRequestedFinalReadback) {
                hasRequestedFinalReadback = true;
                void this.readBackFinalOperation(operationId, currentState, onOperationUpdated);
            }
        };
        this._eventSource.onerror = async () => {
            if (hasRequestedFinalReadback) {
                this.disposeSubscription();
                return;
            }

            hasRequestedFinalReadback = true;
            await this.readBackFinalOperation(operationId, currentState, onOperationUpdated);
        };
    }

    protected async readBackFinalOperation(
        operationId: string,
        fallbackOperation: SearchStudioApiIngestionOperationStateResponse,
        onOperationUpdated: (operation: SearchStudioApiIngestionOperationStateResponse) => void
    ): Promise<void>
    {
        this.disposeSubscription();

        try {
            const latestOperation = await this._apiClient.getOperation(operationId);
            onOperationUpdated(latestOperation);
        } catch {
            onOperationUpdated(fallbackOperation);
        }
    }
}
