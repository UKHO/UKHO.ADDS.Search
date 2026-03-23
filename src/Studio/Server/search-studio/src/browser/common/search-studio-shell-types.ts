import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import type { SearchStudioRuleValidationState } from '../rules/search-studio-rules-types';

export type SearchStudioWorkArea = 'providers' | 'rules' | 'ingestion';

export type SearchStudioProviderTreeNodeKind = 'provider-root' | 'queue' | 'dead-letters';

export interface SearchStudioNodeBadge {
    readonly value: string;
    readonly title: string;
}

export interface SearchStudioProviderTreeNode {
    readonly id: string;
    readonly kind: SearchStudioProviderTreeNodeKind;
    readonly label: string;
    readonly description?: string;
    readonly provider: SearchStudioApiProviderDescriptor;
    readonly badge?: SearchStudioNodeBadge;
    readonly children: readonly SearchStudioProviderTreeNode[];
}

export interface SearchStudioProviderCatalogSnapshot {
    readonly status: 'idle' | 'loading' | 'ready' | 'error';
    readonly providers: readonly SearchStudioApiProviderDescriptor[];
    readonly providerNodes: readonly SearchStudioProviderTreeNode[];
    readonly errorMessage?: string;
}

export type SearchStudioDocumentKind =
    | 'provider-overview'
    | 'provider-queue'
    | 'provider-dead-letters'
    | 'rules-overview'
    | 'rule-checker'
    | 'rule-editor'
    | 'new-rule-editor'
    | 'ingestion-overview'
    | 'ingestion-by-id'
    | 'ingestion-all-unindexed'
    | 'ingestion-by-context';

export type SearchStudioDocumentActionId =
    | 'refresh-providers'
    | 'refresh-rules'
    | 'open-provider-overview'
    | 'open-provider-queue'
    | 'open-provider-dead-letters'
    | 'open-rules-overview'
    | 'open-rule-checker'
    | 'open-new-rule'
    | 'open-ingestion-overview'
    | 'open-ingestion-by-id'
    | 'open-ingestion-all-unindexed'
    | 'open-ingestion-by-context'
    | 'reset-ingestion-status';

export interface SearchStudioDocumentRuleSummary {
    readonly id: string;
    readonly title: string;
    readonly context?: string;
    readonly enabled: boolean;
    readonly validationState: SearchStudioRuleValidationState;
}

export interface SearchStudioDocumentMetric {
    readonly label: string;
    readonly value: string;
    readonly emphasis?: 'default' | 'placeholder';
}

export interface SearchStudioDocumentMetadataItem {
    readonly label: string;
    readonly value: string;
}

export interface SearchStudioDocumentAction {
    readonly id: SearchStudioDocumentActionId;
    readonly label: string;
    readonly appearance?: 'primary' | 'secondary';
}

export interface SearchStudioDocumentOptions {
    readonly documentId: string;
    readonly label: string;
    readonly caption: string;
    readonly iconClass: string;
    readonly kind: SearchStudioDocumentKind;
    readonly provider: SearchStudioApiProviderDescriptor;
    readonly placeholderLabel: string;
    readonly description: string;
    readonly metrics: readonly SearchStudioDocumentMetric[];
    readonly metadata: readonly SearchStudioDocumentMetadataItem[];
    readonly actions: readonly SearchStudioDocumentAction[];
    readonly ruleSummary?: SearchStudioDocumentRuleSummary;
}

export interface SearchStudioOutputEntry {
    readonly id: string;
    readonly timestamp: string;
    readonly level: 'info' | 'error';
    readonly source: string;
    readonly message: string;
}
