import {
    SearchStudioApiProviderDescriptor,
    SearchStudioApiRuleDiscoveryResponse,
    SearchStudioApiRuleSummaryResponse
} from '../api/search-studio-api-types';
import { SearchStudioRulesCatalogSnapshot, SearchStudioRuleNode, SearchStudioRulesProviderGroup, SearchStudioRulesProviderSummary } from './search-studio-rules-types';

export function mapRuleDiscoveryResponseToRulesSnapshot(
    response: SearchStudioApiRuleDiscoveryResponse
): SearchStudioRulesCatalogSnapshot
{
    return {
        status: 'ready',
        schemaVersion: response.schemaVersion,
        providers: response.providers.map(provider => mapProviderRulesToGroup(provider))
    };
}

function mapProviderRulesToGroup(provider: {
    readonly providerName: string;
    readonly displayName: string;
    readonly description?: string;
    readonly rules: readonly SearchStudioApiRuleSummaryResponse[];
}): SearchStudioRulesProviderGroup
{
    const providerDescriptor: SearchStudioApiProviderDescriptor = {
        name: provider.providerName,
        displayName: provider.displayName,
        description: provider.description
    };

    const ruleNodes = provider.rules.map(rule => mapRuleToNode(providerDescriptor, rule));
    const summary = createRulesProviderSummary(provider.rules);

    const ruleCheckerNode: SearchStudioRuleNode = {
        id: `rules:${provider.providerName}:checker`,
        kind: 'rule-checker',
        label: 'Rule checker',
        description: 'Placeholder rule checker surface.',
        provider: providerDescriptor,
        badge: {
            value: 'CHK',
            title: 'Opens the provider-scoped rule checker placeholder.'
        },
        children: []
    };

    const rulesGroupNode: SearchStudioRuleNode = {
        id: `rules:${provider.providerName}:group`,
        kind: 'rules-group',
        label: 'Rules',
        description: `${summary.totalRuleCount} rule(s) for ${provider.displayName}.`,
        provider: providerDescriptor,
        badge: {
            value: `${summary.totalRuleCount}`,
            title: 'Total rule count loaded from StudioApiHost /rules.'
        },
        children: ruleNodes
    };

    const rootNode: SearchStudioRuleNode = {
        id: `rules:${provider.providerName}`,
        kind: 'provider-root',
        label: provider.displayName,
        description: provider.description,
        provider: providerDescriptor,
        badge: {
            value: `${summary.activeRuleCount}/${summary.totalRuleCount}`,
            title: 'Active rules / total rules.'
        },
        children: [ruleCheckerNode, rulesGroupNode]
    };

    return {
        provider: providerDescriptor,
        summary,
        rootNode,
        ruleCheckerNode,
        rulesGroupNode,
        ruleNodes
    };
}

function mapRuleToNode(
    provider: SearchStudioApiProviderDescriptor,
    rule: SearchStudioApiRuleSummaryResponse
): SearchStudioRuleNode
{
    return {
        id: `rules:${provider.name}:rule:${rule.id}`,
        kind: 'rule',
        label: rule.title,
        description: rule.description,
        provider,
        ruleId: rule.id,
        context: rule.context,
        enabled: rule.enabled,
        validationState: 'valid',
        badge: rule.enabled
            ? {
                value: 'ACTIVE',
                title: 'Rule is enabled.'
            }
            : {
                value: 'DISABLED',
                title: 'Rule is currently disabled.'
            },
        children: []
    };
}

function createRulesProviderSummary(rules: readonly SearchStudioApiRuleSummaryResponse[]): SearchStudioRulesProviderSummary {
    const activeRuleCount = rules.filter(rule => rule.enabled).length;
    const disabledRuleCount = rules.length - activeRuleCount;

    return {
        totalRuleCount: rules.length,
        activeRuleCount,
        disabledRuleCount,
        invalidRuleCount: 0,
        invalidRuleCountIsPlaceholder: true
    };
}
