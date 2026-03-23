import type { SearchStudioApiIngestionContextResponse } from '../api/search-studio-api-types';
import type { SearchStudioApiIngestionOperationStateResponse } from '../api/search-studio-api-types';
import { hasActiveOperation } from './search-studio-ingestion-operation-state';

export interface SearchStudioIngestionByContextViewState {
    readonly selectedProviderName?: string;
    readonly contexts: readonly SearchStudioApiIngestionContextResponse[];
    readonly selectedContextValue?: string;
    readonly activeOperation?: SearchStudioApiIngestionOperationStateResponse;
    readonly isLoadingContexts: boolean;
    readonly isRecovering: boolean;
    readonly isStarting: boolean;
    readonly isResetting: boolean;
}

export function canLoadContexts(state: SearchStudioIngestionByContextViewState): boolean {
    return Boolean(state.selectedProviderName)
        && !state.isLoadingContexts
        && !state.isRecovering;
}

export function canStartContextOperation(state: SearchStudioIngestionByContextViewState): boolean {
    return Boolean(state.selectedProviderName)
        && Boolean(state.selectedContextValue)
        && state.contexts.length > 0
        && !hasActiveOperation(state.activeOperation)
        && !state.isLoadingContexts
        && !state.isRecovering
        && !state.isStarting
        && !state.isResetting;
}

export function canResetContextOperation(state: SearchStudioIngestionByContextViewState): boolean {
    return canStartContextOperation(state);
}

export function getSelectedContextDisplayName(
    contexts: readonly SearchStudioApiIngestionContextResponse[],
    selectedContextValue?: string
): string | undefined
{
    return contexts.find(context => context.value === selectedContextValue)?.displayName;
}
