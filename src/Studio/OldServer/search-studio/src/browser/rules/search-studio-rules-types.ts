import type { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import type { SearchStudioNodeBadge } from '../common/search-studio-shell-types';

export type SearchStudioRuleTreeNodeKind = 'provider-root' | 'rule-checker' | 'rules-group' | 'rule';
export type SearchStudioRuleValidationState = 'valid' | 'invalid';

export interface SearchStudioRuleNode {
    readonly id: string;
    readonly kind: SearchStudioRuleTreeNodeKind;
    readonly label: string;
    readonly description?: string;
    readonly provider: SearchStudioApiProviderDescriptor;
    readonly ruleId?: string;
    readonly context?: string;
    readonly enabled?: boolean;
    readonly validationState?: SearchStudioRuleValidationState;
    readonly badge?: SearchStudioNodeBadge;
    readonly children: readonly SearchStudioRuleNode[];
}

export interface SearchStudioRulesProviderSummary {
    readonly totalRuleCount: number;
    readonly activeRuleCount: number;
    readonly disabledRuleCount: number;
    readonly invalidRuleCount: number;
    readonly invalidRuleCountIsPlaceholder: boolean;
}

export interface SearchStudioRulesProviderGroup {
    readonly provider: SearchStudioApiProviderDescriptor;
    readonly summary: SearchStudioRulesProviderSummary;
    readonly rootNode: SearchStudioRuleNode;
    readonly ruleCheckerNode: SearchStudioRuleNode;
    readonly rulesGroupNode: SearchStudioRuleNode;
    readonly ruleNodes: readonly SearchStudioRuleNode[];
}

export interface SearchStudioRulesCatalogSnapshot {
    readonly status: 'idle' | 'loading' | 'ready' | 'error';
    readonly schemaVersion?: string;
    readonly providers: readonly SearchStudioRulesProviderGroup[];
    readonly errorMessage?: string;
}
