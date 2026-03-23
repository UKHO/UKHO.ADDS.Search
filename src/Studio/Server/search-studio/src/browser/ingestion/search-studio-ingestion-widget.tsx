import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ContextMenuRenderer } from '@theia/core/lib/browser/context-menu-renderer';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { CommandRegistry } from '@theia/core/lib/common';
import { SearchStudioProviderCatalogService } from '../api/search-studio-provider-catalog-service';
import { SearchStudioDocumentService } from '../common/search-studio-document-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import {
    SearchStudioIngestionModeContextMenuPath,
    SearchStudioIngestionRootContextMenuPath,
    SearchStudioRefreshProvidersCommand
} from '../search-studio-constants';
import {
    SearchStudioIngestionWidgetIconClass,
    SearchStudioIngestionWidgetId,
    SearchStudioIngestionWidgetLabel
} from '../search-studio-constants';
import { mapProvidersToIngestionProviderGroups } from './search-studio-ingestion-mapper';
import { SearchStudioIngestionNode, SearchStudioIngestionProviderGroup } from './search-studio-ingestion-types';

@injectable()
export class SearchStudioIngestionWidget extends ReactWidget {

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(CommandRegistry)
    protected readonly _commandRegistry!: CommandRegistry;

    @inject(ContextMenuRenderer)
    protected readonly _contextMenuRenderer!: ContextMenuRenderer;

    constructor() {
        super();
        this.id = SearchStudioIngestionWidgetId;
        this.title.label = SearchStudioIngestionWidgetLabel;
        this.title.caption = 'Ingestion work area';
        this.title.iconClass = SearchStudioIngestionWidgetIconClass;
        this.title.closable = false;
        this.addClass('search-studio-ingestion-widget');
    }

    @postConstruct()
    protected init(): void {
        this.toDispose.push(this._providerCatalogService.onDidChange(() => {
            this._providerSelectionService.synchronizeProviderSelection(
                this._providerCatalogService.snapshot.providers,
                'ingestion');
            this.update();
        }));
        this.toDispose.push(this._providerSelectionService.onDidChangeSelectedProvider(() => this.update()));
        void this._providerCatalogService.ensureLoaded();
        this.update();
    }

    protected render(): React.ReactNode {
        const snapshot = this._providerCatalogService.snapshot;
        const providerGroups = mapProvidersToIngestionProviderGroups(snapshot.providers);
        const selectedProvider = this._providerCatalogService.findProvider(this._providerSelectionService.selectedProviderName ?? '');

        return (
            <div style={{ padding: '12px', display: 'grid', gap: '12px' }}>
                <div style={{ display: 'grid', gap: '8px' }}>
                    <div>
                        <strong>{SearchStudioIngestionWidgetLabel}</strong>
                        <div style={{ marginTop: '4px', color: 'var(--theia-descriptionForeground)' }}>
                            Provider-scoped ingestion navigation reuses live provider metadata while all overview and execution surfaces remain intentionally placeholder-driven in this work item.
                        </div>
                        {selectedProvider ? (
                            <div style={{ marginTop: '4px', color: 'var(--theia-descriptionForeground)' }}>
                                Current provider: <strong>{selectedProvider.displayName}</strong>
                            </div>
                        ) : undefined}
                    </div>
                    <div>
                        <button
                            type="button"
                            className="theia-button"
                            onClick={() => this._commandRegistry.executeCommand(SearchStudioRefreshProvidersCommand.id)}
                        >
                            Refresh providers
                        </button>
                    </div>
                </div>
                {snapshot.status === 'error' ? (
                    <div>{snapshot.errorMessage}</div>
                ) : undefined}
                {snapshot.status === 'loading' ? (
                    <div>Loading Studio providers...</div>
                ) : undefined}
                <div style={{ display: 'grid', gap: '8px' }}>
                    {providerGroups.map(group => this.renderProviderGroup(group))}
                </div>
            </div>
        );
    }

    protected renderProviderGroup(group: SearchStudioIngestionProviderGroup): React.ReactNode {
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
                onContextMenu={event => this.renderIngestionRootContextMenu(event, group.provider.name)}
            >
                <button
                    type="button"
                    className="theia-button"
                    style={{ textAlign: 'left', justifyContent: 'flex-start' }}
                    onClick={() => void this.openIngestionOverview(group.provider.name)}
                >
                    <span className="codicon codicon-cloud-upload" style={{ marginRight: '8px' }} />
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
                    {group.rootNode.children.map(child => this.renderModeNode(child))}
                </div>
            </div>
        );
    }

    protected renderModeNode(node: SearchStudioIngestionNode): React.ReactNode {
        return (
            <button
                key={node.id}
                type="button"
                className="theia-button"
                style={{ textAlign: 'left', justifyContent: 'space-between', paddingLeft: '24px' }}
                onClick={() => void this.openMode(node.provider.name, node.kind)}
                onContextMenu={event => this.renderIngestionModeContextMenu(event, node.provider.name, node.kind)}
            >
                <span style={{ display: 'grid', gap: '4px' }}>
                    <span>{node.label}</span>
                    <span style={{ fontSize: '0.8rem', color: 'var(--theia-descriptionForeground)' }}>
                        {node.description}
                    </span>
                </span>
                <span
                    title={node.badge?.title}
                    style={{
                        marginLeft: '8px',
                        padding: '2px 8px',
                        borderRadius: '999px',
                        background: 'var(--theia-badge-background)',
                        color: 'var(--theia-badge-foreground)',
                        fontSize: '0.75rem'
                    }}
                >
                    {node.badge?.value}
                </span>
            </button>
        );
    }

    protected async openIngestionOverview(providerName: string): Promise<void> {
        const provider = this._providerCatalogService.findProvider(providerName);

        if (!provider) {
            return;
        }

        this._providerSelectionService.selectProvider(provider, 'ingestion');
        await this._documentService.openIngestionOverview(provider);
    }

    protected async openMode(providerName: string, mode: SearchStudioIngestionNode['kind']): Promise<void> {
        const provider = this._providerCatalogService.findProvider(providerName);

        if (!provider) {
            return;
        }

        this._providerSelectionService.selectProvider(provider, 'ingestion');

        switch (mode) {
            case 'by-id':
                await this._documentService.openIngestionById(provider);
                return;
            case 'all-unindexed':
                await this._documentService.openIngestionAllUnindexed(provider);
                return;
            case 'by-context':
                await this._documentService.openIngestionByContext(provider);
                return;
        }
    }

    protected renderIngestionRootContextMenu(event: React.MouseEvent<HTMLElement>, providerName: string): void {
        event.preventDefault();
        event.stopPropagation();
        this._contextMenuRenderer.render({
            menuPath: SearchStudioIngestionRootContextMenuPath,
            anchor: event.nativeEvent,
            args: [providerName],
            includeAnchorArg: false,
            context: this.node
        });
    }

    protected renderIngestionModeContextMenu(
        event: React.MouseEvent<HTMLElement>,
        providerName: string,
        mode: SearchStudioIngestionNode['kind']
    ): void
    {
        event.preventDefault();
        event.stopPropagation();
        this._contextMenuRenderer.render({
            menuPath: SearchStudioIngestionModeContextMenuPath,
            anchor: event.nativeEvent,
            args: [providerName, mode],
            includeAnchorArg: false,
            context: this.node
        });
    }
}
