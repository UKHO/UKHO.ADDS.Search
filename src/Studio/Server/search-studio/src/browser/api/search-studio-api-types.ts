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
