import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { ContextMenuRenderer } from '@theia/core/lib/browser/context-menu-renderer';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { SearchStudioProviderCatalogService } from './api/search-studio-provider-catalog-service';
import { SearchStudioDocumentService } from './common/search-studio-document-service';
import { SearchStudioProviderSelectionService } from './common/search-studio-provider-selection-service';
import {
    SearchStudioProvidersContextMenuPath,
    SearchStudioRefreshProvidersCommand,
    SearchStudioWidgetIconClass,
    SearchStudioWidgetId,
    SearchStudioWidgetLabel
} from './search-studio-constants';
import { CommandRegistry } from '@theia/core/lib/common';

@injectable()
export class SearchStudioWidget extends ReactWidget {

    @inject(CommandRegistry)
    protected readonly _commandRegistry!: CommandRegistry;

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(ContextMenuRenderer)
    protected readonly _contextMenuRenderer!: ContextMenuRenderer;

    constructor() {
        super();
        this.id = SearchStudioWidgetId;
        this.title.label = SearchStudioWidgetLabel;
        this.title.caption = 'Provider operations';
        this.title.iconClass = SearchStudioWidgetIconClass;
        this.title.closable = false;
        this.addClass('search-studio-providers-widget');
    }

    @postConstruct()
    protected init(): void {
        this.toDispose.push(this._providerCatalogService.onDidChange(() => {
            this._providerSelectionService.synchronizeProviderSelection(
                this._providerCatalogService.snapshot.providers,
                'providers');
            this.update();
        }));
        this.toDispose.push(this._providerSelectionService.onDidChangeSelectedProvider(() => this.update()));
        void this._providerCatalogService.ensureLoaded();
        this.update();
    }

    protected render(): React.ReactNode {
        const snapshot = this._providerCatalogService.snapshot;
        const selectedProvider = this._providerCatalogService.findProvider(this._providerSelectionService.selectedProviderName ?? '');

        return (
            <div style={{ padding: '12px', display: 'grid', gap: '12px' }}>
                <div style={{ display: 'grid', gap: '4px' }}>
                    <strong>{SearchStudioWidgetLabel}</strong>
                    <div style={{ color: 'var(--theia-descriptionForeground)' }}>
                        Provider metadata loads from `StudioApiHost /providers` while overview and inspector documents remain placeholders in this skeleton.
                    </div>
                    {selectedProvider ? (
                        <div style={{ color: 'var(--theia-descriptionForeground)' }}>
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
                        {SearchStudioRefreshProvidersCommand.label}
                    </button>
                </div>
                {snapshot.status === 'loading' ? <div>Loading Studio providers...</div> : undefined}
                {snapshot.status === 'error' ? (
                    <div style={{ color: 'var(--theia-errorForeground)' }}>{snapshot.errorMessage}</div>
                ) : undefined}
                {snapshot.status === 'ready' && snapshot.providerNodes.length === 0 ? (
                    <div>No providers were returned by StudioApiHost.</div>
                ) : undefined}
                <div style={{ display: 'grid', gap: '8px' }}>
                    {snapshot.providerNodes.map(node => this.renderProviderNode(node.provider.name))}
                </div>
            </div>
        );
    }

    protected renderProviderNode(providerName: string): React.ReactNode {
        const providerNode = this._providerCatalogService.snapshot.providerNodes.find(node => node.provider.name === providerName);

        if (!providerNode) {
            return undefined;
        }

        const isSelected = this._providerSelectionService.selectedProviderName === providerNode.provider.name;

        return (
            <div
                key={providerNode.id}
                style={{
                    display: 'grid',
                    gap: '6px',
                    padding: '10px',
                    borderRadius: '8px',
                    border: '1px solid var(--theia-panel-border)',
                    background: isSelected ? 'var(--theia-list-activeSelectionBackground)' : undefined,
                    color: isSelected ? 'var(--theia-list-activeSelectionForeground)' : undefined
                }}
                onContextMenu={event => this.renderProviderContextMenu(event, providerNode.provider.name)}
            >
                <button
                    type="button"
                    className="theia-button"
                    style={{ textAlign: 'left', justifyContent: 'flex-start' }}
                    onClick={() => void this.openProviderOverview(providerNode.provider.name)}
                >
                    <span className="codicon codicon-database" style={{ marginRight: '8px' }} />
                    <span>{providerNode.provider.displayName}</span>
                    {providerNode.badge ? (
                        <span
                            title={providerNode.badge.title}
                            style={{
                                marginLeft: '8px',
                                padding: '2px 8px',
                                borderRadius: '999px',
                                background: 'var(--theia-badge-background)',
                                color: 'var(--theia-badge-foreground)',
                                fontSize: '0.8rem'
                            }}
                        >
                            {providerNode.badge.value}
                        </span>
                    ) : undefined}
                </button>
                {providerNode.description ? (
                    <div style={{ fontSize: '0.9rem', color: isSelected ? undefined : 'var(--theia-descriptionForeground)' }}>
                        {providerNode.description}
                    </div>
                ) : undefined}
                <div style={{ display: 'grid', gap: '6px', paddingLeft: '16px' }}>
                    {providerNode.children.map(child => (
                        <button
                            key={child.id}
                            type="button"
                            className="theia-button"
                            style={{ textAlign: 'left', justifyContent: 'flex-start' }}
                            onClick={() => void this.openChildDocument(
                                providerNode.provider.name,
                                child.kind === 'queue' ? 'queue' : 'dead-letters')}
                        >
                            <span className={child.kind === 'queue' ? 'codicon codicon-list-unordered' : 'codicon codicon-archive'} style={{ marginRight: '8px' }} />
                            {child.label}
                        </button>
                    ))}
                </div>
            </div>
        );
    }

    protected async openProviderOverview(providerName: string): Promise<void> {
        const provider = this._providerCatalogService.findProvider(providerName);

        if (!provider) {
            return;
        }

        this._providerSelectionService.selectProvider(provider, 'providers');
        await this._documentService.openProviderOverview(provider);
    }

    protected async openChildDocument(providerName: string, kind: 'queue' | 'dead-letters'): Promise<void> {
        const provider = this._providerCatalogService.findProvider(providerName);

        if (!provider) {
            return;
        }

        this._providerSelectionService.selectProvider(provider, 'providers');

        if (kind === 'queue') {
            await this._documentService.openProviderQueue(provider);
            return;
        }

        await this._documentService.openProviderDeadLetters(provider);
    }

    protected renderProviderContextMenu(event: React.MouseEvent<HTMLElement>, providerName: string): void {
        event.preventDefault();
        event.stopPropagation();
        this._contextMenuRenderer.render({
            menuPath: SearchStudioProvidersContextMenuPath,
            anchor: event.nativeEvent,
            args: [providerName],
            includeAnchorArg: false,
            context: this.node
        });
    }
}
