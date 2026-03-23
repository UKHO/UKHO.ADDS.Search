import * as React from '@theia/core/shared/react';
import { inject, injectable } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { SearchStudioProviderCatalogService } from '../api/search-studio-provider-catalog-service';
import {
    SearchStudioDocumentAction,
    SearchStudioDocumentOptions
} from './search-studio-shell-types';
import { SearchStudioDocumentService } from './search-studio-document-service';

@injectable()
export class SearchStudioDocumentWidget extends ReactWidget {

    protected _document?: SearchStudioDocumentOptions;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    constructor() {
        super();
        this.addClass('search-studio-document-widget');
    }

    setDocument(document: SearchStudioDocumentOptions): void {
        this._document = document;
        this.id = document.documentId;
        this.title.label = document.label;
        this.title.caption = document.caption;
        this.title.iconClass = document.iconClass;
        this.title.closable = true;
        this.update();
    }

    protected render(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        return (
            <div style={{ padding: '16px', display: 'grid', gap: '16px', maxWidth: '960px' }}>
                <header style={{ display: 'grid', gap: '8px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
                        <h1 style={{ margin: 0 }}>{this._document.label}</h1>
                        <span
                            style={{
                                padding: '2px 8px',
                                borderRadius: '999px',
                                background: 'var(--theia-badge-background)',
                                color: 'var(--theia-badge-foreground)'
                            }}
                        >
                            {this._document.placeholderLabel}
                        </span>
                    </div>
                    <p style={{ margin: 0, color: 'var(--theia-descriptionForeground)' }}>
                        {this._document.description}
                    </p>
                </header>
                <section style={{ display: 'grid', gap: '12px', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))' }}>
                    {this._document.metrics.map(metric => (
                        <article
                            key={metric.label}
                            style={{
                                padding: '12px',
                                borderRadius: '8px',
                                border: '1px solid var(--theia-panel-border)',
                                background: metric.emphasis === 'placeholder'
                                    ? 'var(--theia-editor-inactiveSelectionBackground)'
                                    : 'var(--theia-editor-background)'
                            }}
                        >
                            <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>{metric.label}</div>
                            <div style={{ marginTop: '8px', fontSize: '1.15rem', fontWeight: 600 }}>{metric.value}</div>
                        </article>
                    ))}
                </section>
                {this._document.ruleSummary ? (
                    <section style={{ display: 'grid', gap: '8px' }}>
                        <h2 style={{ margin: 0, fontSize: '1rem' }}>Rule summary</h2>
                        <div style={{ display: 'grid', gap: '8px', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))' }}>
                            <div style={{ padding: '12px', borderRadius: '8px', border: '1px solid var(--theia-panel-border)' }}>
                                <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>Rule id</div>
                                <div style={{ marginTop: '6px' }}>{this._document.ruleSummary.id}</div>
                            </div>
                            <div style={{ padding: '12px', borderRadius: '8px', border: '1px solid var(--theia-panel-border)' }}>
                                <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>Context</div>
                                <div style={{ marginTop: '6px' }}>{this._document.ruleSummary.context ?? 'Not specified'}</div>
                            </div>
                            <div style={{ padding: '12px', borderRadius: '8px', border: '1px solid var(--theia-panel-border)' }}>
                                <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>Enabled</div>
                                <div style={{ marginTop: '6px' }}>{this._document.ruleSummary.enabled ? 'Yes' : 'No'}</div>
                            </div>
                            <div style={{ padding: '12px', borderRadius: '8px', border: '1px solid var(--theia-panel-border)' }}>
                                <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>Validation state</div>
                                <div style={{ marginTop: '6px' }}>{this._document.ruleSummary.validationState}</div>
                            </div>
                        </div>
                    </section>
                ) : undefined}
                <section
                    style={{
                        display: 'grid',
                        gap: '6px',
                        padding: '12px',
                        borderRadius: '8px',
                        border: '1px solid var(--theia-panel-border)',
                        background: 'var(--theia-editor-inactiveSelectionBackground)'
                    }}
                >
                    <strong>Placeholder workbench surface</strong>
                    <span style={{ color: 'var(--theia-descriptionForeground)' }}>
                        Navigation and context are real, but this document remains intentionally non-production so the Studio layout and flow can be reviewed before functionality is lifted.
                    </span>
                </section>
                <section style={{ display: 'grid', gap: '8px' }}>
                    <h2 style={{ margin: 0, fontSize: '1rem' }}>Provider metadata</h2>
                    <div style={{ display: 'grid', gap: '8px', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))' }}>
                        {this._document.metadata.map(item => (
                            <div
                                key={item.label}
                                style={{
                                    padding: '12px',
                                    borderRadius: '8px',
                                    border: '1px solid var(--theia-panel-border)'
                                }}
                            >
                                <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>{item.label}</div>
                                <div style={{ marginTop: '6px' }}>{item.value}</div>
                            </div>
                        ))}
                    </div>
                </section>
                <section style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                    {this._document.actions.map(action => this.renderAction(action))}
                </section>
            </div>
        );
    }

    protected renderAction(action: SearchStudioDocumentAction): React.ReactNode {
        return (
            <button
                key={action.id}
                type="button"
                className="theia-button"
                style={{
                    background: action.appearance === 'primary'
                        ? 'var(--theia-button-background)'
                        : undefined,
                    color: action.appearance === 'primary'
                        ? 'var(--theia-button-foreground)'
                        : undefined
                }}
                onClick={() => void this.executeAction(action)}
            >
                {action.label}
            </button>
        );
    }

    protected async executeAction(action: SearchStudioDocumentAction): Promise<void> {
        if (!this._document) {
            return;
        }

        switch (action.id) {
            case 'refresh-providers':
                await this._providerCatalogService.refresh();
                return;
            case 'refresh-rules':
                await this._documentService.refreshRules();
                return;
            case 'open-provider-overview':
                await this._documentService.openProviderOverview(this._document.provider);
                return;
            case 'open-provider-queue':
                await this._documentService.openProviderQueue(this._document.provider);
                return;
            case 'open-provider-dead-letters':
                await this._documentService.openProviderDeadLetters(this._document.provider);
                return;
            case 'open-rules-overview':
                await this._documentService.openRulesOverview(this._document.provider);
                return;
            case 'open-rule-checker':
                await this._documentService.openRuleChecker(this._document.provider);
                return;
            case 'open-new-rule':
                await this._documentService.openNewRuleEditor(this._document.provider);
                return;
            case 'open-ingestion-overview':
                await this._documentService.openIngestionOverview(this._document.provider);
                return;
            case 'open-ingestion-by-id':
                await this._documentService.openIngestionById(this._document.provider);
                return;
            case 'open-ingestion-all-unindexed':
                await this._documentService.openIngestionAllUnindexed(this._document.provider);
                return;
            case 'open-ingestion-by-context':
                await this._documentService.openIngestionByContext(this._document.provider);
                return;
            case 'reset-ingestion-status':
                await this._documentService.runResetIngestionStatusPlaceholder(this._document.provider);
                return;
        }
    }
}
