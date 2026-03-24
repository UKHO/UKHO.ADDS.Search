import { ApplicationShell } from '@theia/core/lib/browser';
import { WidgetManager } from '@theia/core/lib/browser/widget-manager';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import { SearchStudioRulesCatalogService } from '../api/search-studio-rules-catalog-service';
import {
    SearchStudioDocumentMetadataItem,
    SearchStudioDocumentMetric,
    SearchStudioDocumentRuleSummary,
    SearchStudioDocumentOptions
} from './search-studio-shell-types';
import { SearchStudioDocumentWidget } from './search-studio-document-widget';
import { SearchStudioOutputService } from './search-studio-output-service';
import {
    SearchStudioDocumentWidgetFactoryId,
    SearchStudioIngestionAllUnindexedDocumentIconClass,
    SearchStudioIngestionByContextDocumentIconClass,
    SearchStudioIngestionByIdDocumentIconClass,
    SearchStudioIngestionOverviewDocumentIconClass,
    SearchStudioNewRuleDocumentIconClass,
    SearchStudioProviderDeadLettersDocumentIconClass,
    SearchStudioProviderOverviewDocumentIconClass,
    SearchStudioProviderQueueDocumentIconClass,
    SearchStudioRuleCheckerDocumentIconClass,
    SearchStudioRuleEditorDocumentIconClass,
    SearchStudioRulesDocumentIconClass
} from '../search-studio-constants';
import { SearchStudioRuleNode, SearchStudioRulesProviderGroup } from '../rules/search-studio-rules-types';

@injectable()
export class SearchStudioDocumentService {

    @inject(ApplicationShell)
    protected readonly _shell!: ApplicationShell;

    @inject(WidgetManager)
    protected readonly _widgetManager!: WidgetManager;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    @inject(SearchStudioRulesCatalogService)
    protected readonly _rulesCatalogService!: SearchStudioRulesCatalogService;

    async refreshRules(): Promise<void> {
        await this._rulesCatalogService.refresh();
    }

    async openProviderOverview(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.provider-overview.${provider.name}`,
            label: `${provider.name} / Overview`,
            caption: `Provider overview for ${provider.displayName}`,
            iconClass: SearchStudioProviderOverviewDocumentIconClass,
            kind: 'provider-overview',
            provider,
            placeholderLabel: 'Skeleton overview',
            description: 'This provider overview uses live provider metadata from StudioApiHost while queue and dead-letter values remain placeholder content in this work package.',
            metrics: this.createProviderOverviewMetrics(),
            metadata: this.createProviderMetadata(provider),
            actions: [
                { id: 'refresh-providers', label: 'Refresh providers' },
                { id: 'open-provider-queue', label: 'Open Queue', appearance: 'primary' },
                { id: 'open-provider-dead-letters', label: 'Open Dead letters' },
                { id: 'open-rules-overview', label: 'Open Rules' },
                { id: 'open-ingestion-overview', label: 'Open Ingestion' }
            ]
        });
    }

    async openProviderQueue(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.provider-queue.${provider.name}`,
            label: `${provider.name} / Queue`,
            caption: `Queue placeholder for ${provider.displayName}`,
            iconClass: SearchStudioProviderQueueDocumentIconClass,
            kind: 'provider-queue',
            provider,
            placeholderLabel: 'Placeholder inspector',
            description: 'This queue inspector is intentionally mocked for the skeleton so layout, navigation, and document behaviour can be reviewed before queue functionality is lifted into Studio.',
            metrics: [
                { label: 'Queue depth', value: 'Unavailable', emphasis: 'placeholder' },
                { label: 'Last refresh', value: 'Not implemented', emphasis: 'placeholder' }
            ],
            metadata: this.createProviderMetadata(provider),
            actions: [
                { id: 'open-provider-overview', label: 'Open Overview' },
                { id: 'open-provider-dead-letters', label: 'Open Dead letters' },
                { id: 'refresh-providers', label: 'Refresh providers' }
            ]
        });
    }

    async openProviderDeadLetters(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.provider-dead-letters.${provider.name}`,
            label: `${provider.name} / Dead letters`,
            caption: `Dead-letter placeholder for ${provider.displayName}`,
            iconClass: SearchStudioProviderDeadLettersDocumentIconClass,
            kind: 'provider-dead-letters',
            provider,
            placeholderLabel: 'Placeholder inspector',
            description: 'This dead-letter inspector is intentionally mocked for the skeleton so the Studio workbench shape can be reviewed before dead-letter functionality is translated from the existing tooling.',
            metrics: [
                { label: 'Dead letters', value: 'Unavailable', emphasis: 'placeholder' },
                { label: 'Replay actions', value: 'Not implemented', emphasis: 'placeholder' }
            ],
            metadata: this.createProviderMetadata(provider),
            actions: [
                { id: 'open-provider-overview', label: 'Open Overview' },
                { id: 'open-provider-queue', label: 'Open Queue' },
                { id: 'refresh-providers', label: 'Refresh providers' }
            ]
        });
    }

    async openRulesOverview(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this._rulesCatalogService.ensureLoaded();

        const providerGroup = this._rulesCatalogService.findProviderGroup(provider.name);

        if (!providerGroup) {
            await this.openDocument({
                documentId: `search-studio.document.rules-overview.${provider.name}`,
                label: `${provider.name} / Rules overview`,
                caption: `Rules overview placeholder for ${provider.displayName}`,
                iconClass: SearchStudioRulesDocumentIconClass,
                kind: 'rules-overview',
                provider,
                placeholderLabel: 'Rules unavailable',
                description: 'The Rules overview could not load provider-specific rule data from StudioApiHost /rules.',
                metrics: [
                    { label: 'Rules', value: 'Unavailable', emphasis: 'placeholder' },
                    { label: 'Active', value: 'Unavailable', emphasis: 'placeholder' },
                    { label: 'Invalid', value: 'Unavailable', emphasis: 'placeholder' }
                ],
                metadata: this.createProviderMetadata(provider),
                actions: [
                    { id: 'refresh-rules', label: 'Refresh rules' },
                    { id: 'open-new-rule', label: 'New rule', appearance: 'primary' }
                ]
            });
            return;
        }

        await this.openDocument({
            documentId: `search-studio.document.rules-overview.${provider.name}`,
            label: `${provider.name} / Rules overview`,
            caption: `Rules overview for ${provider.displayName}`,
            iconClass: SearchStudioRulesDocumentIconClass,
            kind: 'rules-overview',
            provider,
            placeholderLabel: 'Skeleton overview',
            description: 'This rules overview uses live StudioApiHost /rules data for discovery while rule editing and checking remain placeholder workbench surfaces in this work package.',
            metrics: this.createRulesOverviewMetrics(providerGroup),
            metadata: this.createRulesOverviewMetadata(providerGroup),
            actions: [
                { id: 'refresh-rules', label: 'Refresh rules' },
                { id: 'open-rule-checker', label: 'Open Rule checker' },
                { id: 'open-new-rule', label: 'New rule', appearance: 'primary' },
                { id: 'open-provider-overview', label: 'Open Provider overview' }
            ]
        });
    }

    async openRuleChecker(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.rule-checker.${provider.name}`,
            label: `${provider.name} / Rule checker`,
            caption: `Rule checker placeholder for ${provider.displayName}`,
            iconClass: SearchStudioRuleCheckerDocumentIconClass,
            kind: 'rule-checker',
            provider,
            placeholderLabel: 'Placeholder checker',
            description: 'This provider-scoped rule checker remains a placeholder surface in this work item, but it is now reachable from a live rules tree and reviewable as part of the Studio workbench design.',
            metrics: [
                { label: 'Rule validation', value: 'Placeholder', emphasis: 'placeholder' },
                { label: 'Current provider', value: provider.displayName }
            ],
            metadata: this.createProviderMetadata(provider),
            actions: [
                { id: 'open-rules-overview', label: 'Open Rules overview' },
                { id: 'open-new-rule', label: 'New rule', appearance: 'primary' },
                { id: 'open-ingestion-overview', label: 'Open Ingestion' }
            ]
        });
    }

    async openRuleEditor(provider: SearchStudioApiProviderDescriptor, ruleNode: SearchStudioRuleNode): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.rule-editor.${provider.name}.${ruleNode.ruleId}`,
            label: `${provider.name} / ${ruleNode.ruleId ?? ruleNode.label}`,
            caption: `Rule placeholder editor for ${ruleNode.label}`,
            iconClass: SearchStudioRuleEditorDocumentIconClass,
            kind: 'rule-editor',
            provider,
            placeholderLabel: 'Placeholder editor',
            description: 'This rule document is opened from live StudioApiHost /rules discovery, while the actual node editor and save workflow remain future work.',
            metrics: this.createRuleEditorMetrics(ruleNode),
            metadata: this.createRuleEditorMetadata(ruleNode),
            actions: [
                { id: 'open-rules-overview', label: 'Open Rules overview' },
                { id: 'open-rule-checker', label: 'Open Rule checker' },
                { id: 'open-new-rule', label: 'New rule' }
            ],
            ruleSummary: this.createDocumentRuleSummary(ruleNode)
        });
    }

    async openNewRuleEditor(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.new-rule.${provider.name}`,
            label: `${provider.name} / New rule`,
            caption: `New rule placeholder for ${provider.displayName}`,
            iconClass: SearchStudioNewRuleDocumentIconClass,
            kind: 'new-rule-editor',
            provider,
            placeholderLabel: 'Placeholder authoring surface',
            description: 'This new-rule editor is intentionally non-persistent in the Studio skeleton. Its purpose is to validate layout, navigation, and authoring affordances before real write support is lifted into Studio.',
            metrics: [
                { label: 'Persistence', value: 'Not implemented', emphasis: 'placeholder' },
                { label: 'Target provider', value: provider.displayName }
            ],
            metadata: this.createProviderMetadata(provider),
            actions: [
                { id: 'open-rules-overview', label: 'Open Rules overview' },
                { id: 'open-rule-checker', label: 'Open Rule checker' },
                { id: 'refresh-rules', label: 'Refresh rules' }
            ]
        });
    }

    async openIngestionOverview(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.ingestion-overview.${provider.name}`,
            label: `${provider.name} / Ingestion overview`,
            caption: `Ingestion overview for ${provider.displayName}`,
            iconClass: SearchStudioIngestionOverviewDocumentIconClass,
            kind: 'ingestion-overview',
            provider,
            placeholderLabel: 'Skeleton overview',
            description: 'This provider-scoped ingestion overview is intentionally placeholder-driven. Its purpose is to review mode separation, dashboard shape, and operational affordances before ingestion functionality is lifted into Studio.',
            metrics: [
                { label: 'Indexed', value: 'Unavailable', emphasis: 'placeholder' },
                { label: 'Non-indexed', value: 'Unavailable', emphasis: 'placeholder' },
                { label: 'Mode separation', value: 'Review-ready', emphasis: 'placeholder' }
            ],
            metadata: [
                ...this.createProviderMetadata(provider),
                { label: 'Dashboard source', value: 'Placeholder until a dedicated ingestion API exists' }
            ],
            actions: [
                { id: 'open-ingestion-by-id', label: 'Open By id', appearance: 'primary' },
                { id: 'open-ingestion-all-unindexed', label: 'Open All unindexed' },
                { id: 'open-ingestion-by-context', label: 'Open By context' },
                { id: 'reset-ingestion-status', label: 'Reset indexing status' },
                { id: 'open-provider-overview', label: 'Open Overview' }
            ]
        });
    }

    async openIngestionById(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.ingestion-by-id.${provider.name}`,
            label: `${provider.name} / By id`,
            caption: `By-id ingestion for ${provider.displayName}`,
            iconClass: SearchStudioIngestionByIdDocumentIconClass,
            kind: 'ingestion-by-id',
            provider,
            placeholderLabel: 'Provider-neutral flow',
            description: 'Fetch a payload by id, inspect the payload JSON, and submit the same payload for indexing without client-side transformation.',
            metrics: [
                { label: 'Identifier input', value: 'Live' },
                { label: 'Payload preview', value: 'Read-only JSON editor' },
                { label: 'Submission', value: 'Direct queue hand-off' }
            ],
            metadata: [
                ...this.createProviderMetadata(provider),
                { label: 'Payload source', value: 'StudioApiHost /ingestion/{provider}/{id}' },
                { label: 'Submission target', value: 'StudioApiHost /ingestion/{provider}/payload' }
            ],
            actions: [
                { id: 'open-ingestion-overview', label: 'Open Ingestion overview' }
            ]
        });
    }

    async openIngestionAllUnindexed(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.ingestion-all-unindexed.${provider.name}`,
            label: `${provider.name} / All unindexed`,
            caption: `All-unindexed ingestion for ${provider.displayName}`,
            iconClass: SearchStudioIngestionAllUnindexedDocumentIconClass,
            kind: 'ingestion-all-unindexed',
            provider,
            placeholderLabel: 'Live operation flow',
            description: 'Start provider-wide ingestion for all unindexed items, monitor coarse progress, reset indexing status, and recover the active operation after reload.',
            metrics: [
                { label: 'Scope', value: 'All unindexed items' },
                { label: 'Progress', value: 'Live SSE updates' },
                { label: 'Recovery', value: 'Active operation lookup' }
            ],
            metadata: [
                ...this.createProviderMetadata(provider),
                { label: 'Start target', value: 'StudioApiHost /ingestion/{provider}/all' },
                { label: 'Reset target', value: 'StudioApiHost /ingestion/{provider}/operations/reset-indexing-status' },
                { label: 'Recovery source', value: 'StudioApiHost /operations/active and /operations/{operationId}/events' }
            ],
            actions: [
                { id: 'open-ingestion-overview', label: 'Open Ingestion overview' },
                { id: 'reset-ingestion-status', label: 'Reset indexing status' }
            ]
        });
    }

    async openIngestionByContext(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        await this.openDocument({
            documentId: `search-studio.document.ingestion-by-context.${provider.name}`,
            label: `${provider.name} / By context`,
            caption: `By-context ingestion for ${provider.displayName}`,
            iconClass: SearchStudioIngestionByContextDocumentIconClass,
            kind: 'ingestion-by-context',
            provider,
            placeholderLabel: 'Live context flow',
            description: 'Load provider contexts, select one by display name, start context-scoped ingestion or reset, and track progress using the shared operation model.',
            metrics: [
                { label: 'Context selector', value: 'Live API discovery' },
                { label: 'Scope', value: 'Context-scoped operations' },
                { label: 'Progress', value: 'Shared operation tracking' }
            ],
            metadata: [
                ...this.createProviderMetadata(provider),
                { label: 'Context source', value: 'StudioApiHost /ingestion/{provider}/contexts' },
                { label: 'Start target', value: 'StudioApiHost /ingestion/{provider}/context/{context}' },
                { label: 'Reset target', value: 'StudioApiHost /ingestion/{provider}/context/{context}/operations/reset-indexing-status' }
            ],
            actions: [
                { id: 'open-ingestion-overview', label: 'Open Ingestion overview' },
                { id: 'reset-ingestion-status', label: 'Reset indexing status' }
            ]
        });
    }

    async runResetIngestionStatusPlaceholder(provider: SearchStudioApiProviderDescriptor): Promise<void> {
        this._outputService.info(`Reset indexing status requested for '${provider.name}' (placeholder only).`, 'ingestion');
    }

    protected async openDocument(options: SearchStudioDocumentOptions): Promise<void> {
        const widget = await this._widgetManager.getOrCreateWidget<SearchStudioDocumentWidget>(
            SearchStudioDocumentWidgetFactoryId,
            options);

        if (!widget.isAttached) {
            await this._shell.addWidget(widget, {
                area: 'main'
            });
        }

        await this._shell.activateWidget(widget.id);
        this._outputService.info(`Opened ${options.label}.`, 'documents');
    }

    protected createProviderOverviewMetrics(): readonly SearchStudioDocumentMetric[] {
        return [
            { label: 'Queue count', value: 'Unavailable', emphasis: 'placeholder' },
            { label: 'Dead letters', value: 'Unavailable', emphasis: 'placeholder' }
        ];
    }

    protected createProviderMetadata(provider: SearchStudioApiProviderDescriptor): readonly SearchStudioDocumentMetadataItem[] {
        return [
            { label: 'Provider key', value: provider.name },
            { label: 'Display name', value: provider.displayName },
            { label: 'Description', value: provider.description ?? 'No description provided.' },
            { label: 'Metadata source', value: 'StudioApiHost /providers' }
        ];
    }

    protected createRulesOverviewMetrics(providerGroup: SearchStudioRulesProviderGroup): readonly SearchStudioDocumentMetric[] {
        return [
            { label: 'Rules', value: `${providerGroup.summary.totalRuleCount}` },
            { label: 'Active', value: `${providerGroup.summary.activeRuleCount}` },
            {
                label: 'Invalid',
                value: providerGroup.summary.invalidRuleCountIsPlaceholder
                    ? `${providerGroup.summary.invalidRuleCount} (startup fails on invalid rules)`
                    : `${providerGroup.summary.invalidRuleCount}`,
                emphasis: providerGroup.summary.invalidRuleCountIsPlaceholder ? 'placeholder' : 'default'
            }
        ];
    }

    protected createRulesOverviewMetadata(providerGroup: SearchStudioRulesProviderGroup): readonly SearchStudioDocumentMetadataItem[] {
        return [
            ...this.createProviderMetadata(providerGroup.provider),
            { label: 'Rules source', value: 'StudioApiHost /rules' },
            { label: 'Disabled rules', value: `${providerGroup.summary.disabledRuleCount}` },
            { label: 'Versioning', value: 'Reserved placeholder for future work' }
        ];
    }

    protected createRuleEditorMetrics(ruleNode: SearchStudioRuleNode): readonly SearchStudioDocumentMetric[] {
        return [
            { label: 'Rule id', value: ruleNode.ruleId ?? 'Unavailable' },
            { label: 'Context', value: ruleNode.context ?? 'Not specified', emphasis: ruleNode.context ? 'default' : 'placeholder' },
            { label: 'Status', value: ruleNode.enabled ? 'Active' : 'Disabled' }
        ];
    }

    protected createRuleEditorMetadata(ruleNode: SearchStudioRuleNode): readonly SearchStudioDocumentMetadataItem[] {
        return [
            ...this.createProviderMetadata(ruleNode.provider),
            { label: 'Rule title', value: ruleNode.label },
            { label: 'Rule description', value: ruleNode.description ?? 'No description provided.' },
            { label: 'Validation state', value: ruleNode.validationState ?? 'Unknown' }
        ];
    }

    protected createDocumentRuleSummary(ruleNode: SearchStudioRuleNode): SearchStudioDocumentRuleSummary {
        return {
            id: ruleNode.ruleId ?? ruleNode.label,
            title: ruleNode.label,
            context: ruleNode.context,
            enabled: ruleNode.enabled ?? false,
            validationState: ruleNode.validationState ?? 'valid'
        };
    }
}
