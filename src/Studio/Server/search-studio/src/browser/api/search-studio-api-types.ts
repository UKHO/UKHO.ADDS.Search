export interface SearchStudioApiProviderDescriptor {
    readonly name: string;
    readonly displayName: string;
    readonly description?: string;
}

export interface SearchStudioApiRuleSummaryResponse {
    readonly id: string;
    readonly context?: string;
    readonly title: string;
    readonly description?: string;
    readonly enabled: boolean;
}

export interface SearchStudioApiProviderRulesResponse {
    readonly providerName: string;
    readonly displayName: string;
    readonly description?: string;
    readonly rules: readonly SearchStudioApiRuleSummaryResponse[];
}

export interface SearchStudioApiRuleDiscoveryResponse {
    readonly schemaVersion: string;
    readonly providers: readonly SearchStudioApiProviderRulesResponse[];
}

export interface SearchStudioApiIngestionPayloadEnvelope {
    readonly id: string;
    readonly payload: unknown;
}

export interface SearchStudioApiIngestionSubmitPayloadRequest {
    readonly id: string;
    readonly payload: unknown;
}

export interface SearchStudioApiIngestionSubmitPayloadResponse {
    readonly accepted: boolean;
    readonly message: string;
}

export interface SearchStudioApiIngestionContextResponse {
    readonly value: string;
    readonly displayName: string;
    readonly isDefault: boolean;
}

export interface SearchStudioApiIngestionContextsResponse {
    readonly provider: string;
    readonly contexts: readonly SearchStudioApiIngestionContextResponse[];
}

export type SearchStudioApiIngestionOperationStatus = 'queued' | 'running' | 'succeeded' | 'failed';

export interface SearchStudioApiIngestionAcceptedOperationResponse {
    readonly operationId: string;
    readonly provider: string;
    readonly operationType: string;
    readonly context?: string | null;
    readonly status: SearchStudioApiIngestionOperationStatus;
}

export interface SearchStudioApiIngestionOperationStateResponse {
    readonly operationId: string;
    readonly provider: string;
    readonly operationType: string;
    readonly context?: string | null;
    readonly status: SearchStudioApiIngestionOperationStatus;
    readonly message: string;
    readonly completed?: number | null;
    readonly total?: number | null;
    readonly startedUtc: string;
    readonly completedUtc?: string | null;
    readonly failureCode?: string | null;
}

export interface SearchStudioApiIngestionOperationEventResponse {
    readonly eventType: string;
    readonly operationId: string;
    readonly status: SearchStudioApiIngestionOperationStatus;
    readonly message: string;
    readonly completed?: number | null;
    readonly total?: number | null;
    readonly timestampUtc: string;
    readonly failureCode?: string | null;
}

export interface SearchStudioApiIngestionOperationConflictResponse {
    readonly message: string;
    readonly activeOperationId: string;
    readonly activeProvider: string;
    readonly activeOperationType: string;
}
