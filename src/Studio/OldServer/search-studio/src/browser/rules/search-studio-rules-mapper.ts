import { CompositeTreeNode } from '@theia/core/lib/browser/tree/tree';
import {
    SearchStudioApiProviderDescriptor,
    SearchStudioApiRuleDiscoveryResponse,
    SearchStudioApiRuleSummaryResponse
} from '../api/search-studio-api-types';
import {
    createSearchStudioCompositeTreeNode,
    createSearchStudioTreeLeafNode,
    createSearchStudioTreeRoot
} from '../common/search-studio-tree-types';
import { SearchStudioRulesCatalogSnapshot, SearchStudioRuleNode, SearchStudioRulesProviderGroup, SearchStudioRulesProviderSummary } from './search-studio-rules-types';
import {
    SearchStudioRulesCompositeTreeNode,
    SearchStudioRulesTreeNode,
    SearchStudioRulesTreeRoot
} from './search-studio-rules-tree-types';

export type SearchStudioRulesTreeOpenTarget = 'rules-overview' | 'rule-checker' | 'rule-editor';

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

export function mapRulesCatalogSnapshotToRulesTreeRoot(
    snapshot: SearchStudioRulesCatalogSnapshot
): SearchStudioRulesTreeRoot
{
    const root = createSearchStudioTreeRoot('search-studio.rules.root', 'Rules') as SearchStudioRulesTreeRoot;
    const children = mapRulesCatalogSnapshotToTreeNodes(snapshot);

    CompositeTreeNode.addChildren(root, [...children]);

    return root;
}

export function resolveRulesTreeNodeOpenTarget(
    node: Pick<SearchStudioRulesTreeNode, 'kind'>
): SearchStudioRulesTreeOpenTarget | undefined
{
    switch (node.kind) {
        case 'provider-root':
        case 'rules-group':
            return 'rules-overview';
        case 'rule-checker':
            return 'rule-checker';
        case 'rule':
            return 'rule-editor';
        default:
            return undefined;
    }
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

function mapRulesCatalogSnapshotToTreeNodes(
    snapshot: SearchStudioRulesCatalogSnapshot
): readonly SearchStudioRulesTreeNode[]
{
    switch (snapshot.status) {
        case 'loading':
            return [createStatusNode('search-studio.rules.status.loading', 'Loading Studio rules...', 'codicon codicon-loading')];
        case 'error':
            return [createStatusNode('search-studio.rules.status.error', snapshot.errorMessage ?? 'Studio rule discovery could not be loaded.', 'codicon codicon-error')];
        case 'ready':
            if (snapshot.providers.length === 0) {
                return [createStatusNode('search-studio.rules.status.empty', 'No rules were returned by StudioApiHost.', 'codicon codicon-info')];
            }

            return snapshot.providers.map(group => mapRuleTreeNodeToRulesTreeNode(group.rootNode));
        case 'idle':
        default:
            return [createStatusNode('search-studio.rules.status.idle', 'Loading Studio rules...', 'codicon codicon-loading')];
    }
}

function mapRuleTreeNodeToRulesTreeNode(node: SearchStudioRuleNode): SearchStudioRulesTreeNode {
    const commonNodeProperties = {
        id: node.id,
        kind: node.kind,
        label: node.label,
        iconClass: getRulesTreeNodeIcon(node.kind),
        description: node.description,
        data: {
            provider: node.provider,
            ruleNode: node.kind === 'rule' ? node : undefined
        }
    } as const;

    if (node.children.length === 0) {
        return createSearchStudioTreeLeafNode(commonNodeProperties) as SearchStudioRulesTreeNode;
    }

    return createSearchStudioCompositeTreeNode({
        ...commonNodeProperties,
        children: node.children.map(child => mapRuleTreeNodeToRulesTreeNode(child))
    }) as SearchStudioRulesCompositeTreeNode;
}

function createStatusNode(
    id: string,
    label: string,
    iconClass: string
): SearchStudioRulesTreeNode
{
    return createSearchStudioTreeLeafNode({
        id,
        kind: 'status',
        label,
        iconClass
    }) as SearchStudioRulesTreeNode;
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

function getRulesTreeNodeIcon(kind: SearchStudioRuleNode['kind']): string {
    switch (kind) {
        case 'provider-root':
            return 'codicon codicon-symbol-field';
        case 'rule-checker':
            return 'codicon codicon-symbol-event';
        case 'rules-group':
            return 'codicon codicon-folder-library';
        case 'rule':
            return 'codicon codicon-symbol-property';
    }
}
