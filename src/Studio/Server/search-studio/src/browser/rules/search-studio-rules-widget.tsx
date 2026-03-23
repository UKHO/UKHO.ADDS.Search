import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ContextMenuRenderer } from '@theia/core/lib/browser/context-menu-renderer';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { CommandRegistry } from '@theia/core/lib/common';
import { SearchStudioProviderCatalogService } from '../api/search-studio-provider-catalog-service';
import { SearchStudioRulesCatalogService } from '../api/search-studio-rules-catalog-service';
import { SearchStudioDocumentService } from '../common/search-studio-document-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import {
    SearchStudioNewRuleCommand,
    SearchStudioRefreshRulesCommand,
    SearchStudioRulesContextMenuPath,
    SearchStudioRulesWidgetIconClass,
    SearchStudioRulesWidgetId,
    SearchStudioRulesWidgetLabel
} from '../search-studio-constants';
import { SearchStudioRuleNode, SearchStudioRulesProviderGroup } from './search-studio-rules-types';

@injectable()
export class SearchStudioRulesWidget extends ReactWidget {

    @inject(SearchStudioRulesCatalogService)
    protected readonly _rulesCatalogService!: SearchStudioRulesCatalogService;

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(CommandRegistry)
    protected readonly _commandRegistry!: CommandRegistry;

    @inject(ContextMenuRenderer)
    protected readonly _contextMenuRenderer!: ContextMenuRenderer;

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    constructor() {
        super();
        this.id = SearchStudioRulesWidgetId;
        this.title.label = SearchStudioRulesWidgetLabel;
        this.title.caption = 'Rules work area';
        this.title.iconClass = SearchStudioRulesWidgetIconClass;
        this.title.closable = false;
        this.addClass('search-studio-rules-widget');
    }

    @postConstruct()
    protected init(): void {
        this.toDispose.push(this._rulesCatalogService.onDidChange(() => {
            this._providerSelectionService.synchronizeProviderSelection(
                this._rulesCatalogService.snapshot.providers.map(group => group.provider),
                'rules');
            this.update();
        }));
        this.toDispose.push(this._providerSelectionService.onDidChangeSelectedProvider(() => this.update()));
        void this._rulesCatalogService.ensureLoaded();
        this.update();
    }

    protected render(): React.ReactNode {
        const snapshot = this._rulesCatalogService.snapshot;
        const selectedProvider = this._providerCatalogService.findProvider(this._providerSelectionService.selectedProviderName ?? '');

        return (
            <div style={{ padding: '12px', display: 'grid', gap: '12px' }}>
                <div style={{ display: 'grid', gap: '8px' }}>
                    <div>
                        <strong>{SearchStudioRulesWidgetLabel}</strong>
                        <div style={{ marginTop: '4px', color: 'var(--theia-descriptionForeground)' }}>
                            Provider-scoped rules navigation uses live `StudioApiHost /rules` discovery while the checker and editors remain placeholder workbench surfaces.
                        </div>
                        {selectedProvider ? (
                            <div style={{ marginTop: '4px', color: 'var(--theia-descriptionForeground)' }}>
                                Current provider: <strong>{selectedProvider.displayName}</strong>
                            </div>
                        ) : undefined}
                    </div>
                    <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                        <button
                            type="button"
                            className="theia-button"
                            onClick={() => this._commandRegistry.executeCommand(SearchStudioRefreshRulesCommand.id)}
                        >
                            Refresh rules
                        </button>
                        <button
                            type="button"
                            className="theia-button"
                            onClick={() => this._commandRegistry.executeCommand(
                                SearchStudioNewRuleCommand.id,
                                this._providerSelectionService.selectedProviderName)}
                        >
                            New Rule
                        </button>
                    </div>
                </div>
                {snapshot.status === 'error' ? (
                    <div>{snapshot.errorMessage}</div>
                ) : undefined}
                {snapshot.status === 'loading' ? (
                    <div>Loading Studio rules...</div>
                ) : undefined}
                <div style={{ display: 'grid', gap: '8px' }}>
                    {snapshot.providers.map(group => this.renderProviderGroup(group))}
                </div>
            </div>
        );
    }

    protected renderProviderGroup(group: SearchStudioRulesProviderGroup): React.ReactNode {
        const isSelected = this._providerSelectionService.selectedProviderName === group.provider.name;

        return (
            <div
                key={group.rootNode.id}
                style={{
                    display: 'grid',
                    gap: '6px',
                    padding: '10px',
                    borderRadius: '8px',
                    border: '1px solid var(--theia-panel-border)',
                    background: isSelected ? 'var(--theia-list-activeSelectionBackground)' : undefined,
                    color: isSelected ? 'var(--theia-list-activeSelectionForeground)' : undefined
                }}
                onContextMenu={event => this.renderRulesContextMenu(event, group.provider.name)}
            >
                <button
                    type="button"
                    className="theia-button"
                    style={{ textAlign: 'left', justifyContent: 'flex-start' }}
                    onClick={() => void this.openRulesOverview(group.provider.name)}
                >
                    <span className="codicon codicon-symbol-field" style={{ marginRight: '8px' }} />
                    <span>{group.provider.displayName}</span>
                    <span
                        title={group.rootNode.badge?.title}
                        style={{
                            marginLeft: '8px',
                            padding: '2px 8px',
                            borderRadius: '999px',
                            background: 'var(--theia-badge-background)',
                            color: 'var(--theia-badge-foreground)',
                            fontSize: '0.8rem'
                        }}
                    >
                        {group.rootNode.badge?.value}
                    </span>
                </button>
                {group.provider.description ? (
                    <div style={{ fontSize: '0.9rem', color: isSelected ? undefined : 'var(--theia-descriptionForeground)' }}>
                        {group.provider.description}
                    </div>
                ) : undefined}
                <div style={{ display: 'grid', gap: '6px', paddingLeft: '16px' }}>
                    <button
                        type="button"
                        className="theia-button"
                        style={{ textAlign: 'left', justifyContent: 'flex-start' }}
                        onClick={() => void this.openRuleChecker(group.provider.name)}
                    >
                        <span className="codicon codicon-symbol-event" style={{ marginRight: '8px' }} />
                        {group.ruleCheckerNode.label}
                    </button>
                    <div
                        style={{ display: 'grid', gap: '6px' }}
                        onContextMenu={event => this.renderRulesContextMenu(event, group.provider.name)}
                    >
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '4px 0' }}>
                            <span className="codicon codicon-folder-library" />
                            <strong>{group.rulesGroupNode.label}</strong>
                            <span
                                title={group.rulesGroupNode.badge?.title}
                                style={{
                                    padding: '2px 8px',
                                    borderRadius: '999px',
                                    background: 'var(--theia-badge-background)',
                                    color: 'var(--theia-badge-foreground)',
                                    fontSize: '0.8rem'
                                }}
                            >
                                {group.rulesGroupNode.badge?.value}
                            </span>
                        </div>
                        {group.ruleNodes.length === 0 ? (
                            <div style={{ paddingLeft: '24px', color: 'var(--theia-descriptionForeground)' }}>
                                No rules discovered for this provider.
                            </div>
                        ) : (
                            group.ruleNodes.map(ruleNode => this.renderRuleNode(ruleNode))
                        )}
                    </div>
                </div>
            </div>
        );
    }

    protected renderRuleNode(ruleNode: SearchStudioRuleNode): React.ReactNode {
        return (
            <button
                key={ruleNode.id}
                type="button"
                className="theia-button"
                style={{ textAlign: 'left', justifyContent: 'space-between', paddingLeft: '24px' }}
                onClick={() => void this.openRuleEditor(ruleNode.provider.name, ruleNode.id)}
            >
                <span style={{ display: 'grid', gap: '4px' }}>
                    <span>{ruleNode.label}</span>
                    <span style={{ fontSize: '0.8rem', color: 'var(--theia-descriptionForeground)' }}>
                        {ruleNode.context ? `Context: ${ruleNode.context}` : 'No context'}
                    </span>
                </span>
                <span
                    title={ruleNode.badge?.title}
                    style={{
                        marginLeft: '8px',
                        padding: '2px 8px',
                        borderRadius: '999px',
                        background: ruleNode.enabled
                            ? 'var(--theia-badge-background)'
                            : 'var(--theia-editorWarning-foreground)',
                        color: ruleNode.enabled
                            ? 'var(--theia-badge-foreground)'
                            : 'var(--theia-editor-background)',
                        fontSize: '0.75rem'
                    }}
                >
                    {ruleNode.badge?.value}
                </span>
            </button>
        );
    }

    protected async openRulesOverview(providerName: string): Promise<void> {
        const provider = this._providerCatalogService.findProvider(providerName);

        if (!provider) {
            return;
        }

        this._providerSelectionService.selectProvider(provider, 'rules');
        await this._documentService.openRulesOverview(provider);
    }

    protected async openRuleChecker(providerName: string): Promise<void> {
        const provider = this._providerCatalogService.findProvider(providerName);

        if (!provider) {
            return;
        }

        this._providerSelectionService.selectProvider(provider, 'rules');
        await this._documentService.openRuleChecker(provider);
    }

    protected async openRuleEditor(providerName: string, ruleNodeId: string): Promise<void> {
        const providerGroup = this._rulesCatalogService.findProviderGroup(providerName);
        const provider = this._providerCatalogService.findProvider(providerName);

        if (!providerGroup || !provider) {
            return;
        }

        const ruleNode = providerGroup.ruleNodes.find(node => node.id === ruleNodeId);

        if (!ruleNode) {
            return;
        }

        this._providerSelectionService.selectProvider(provider, 'rules');
        await this._documentService.openRuleEditor(provider, ruleNode);
    }

    protected renderRulesContextMenu(event: React.MouseEvent<HTMLElement>, providerName: string): void {
        event.preventDefault();
        event.stopPropagation();
        this._contextMenuRenderer.render({
            menuPath: SearchStudioRulesContextMenuPath,
            anchor: event.nativeEvent,
            args: [providerName],
            includeAnchorArg: false,
            context: this.node
        });
    }
}
