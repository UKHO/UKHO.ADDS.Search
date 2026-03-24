import * as React from '@theia/core/shared/react';
import { inject, injectable } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import {
    SearchStudioApiIngestionContextResponse,
    SearchStudioApiIngestionOperationConflictResponse,
    SearchStudioApiIngestionOperationStateResponse,
    SearchStudioApiIngestionPayloadEnvelope
} from '../api/search-studio-api-types';
import { SearchStudioApiClient, SearchStudioApiRequestError } from '../api/search-studio-api-client';
import { SearchStudioProviderCatalogService } from '../api/search-studio-provider-catalog-service';
import {
    SearchStudioDocumentAction,
    SearchStudioDocumentOptions
} from './search-studio-shell-types';
import { SearchStudioDocumentService } from './search-studio-document-service';
import { SearchStudioOutputService } from './search-studio-output-service';
import {
    canFetchById,
    canSubmitFetchedPayload,
    createSubmitPayloadRequest,
    getPayloadPreviewText,
    mapByIdActionErrorMessage
} from '../ingestion/search-studio-ingestion-by-id-state';
import {
    canLoadContexts,
    canResetContextOperation,
    canStartContextOperation,
    getSelectedContextDisplayName
} from '../ingestion/search-studio-ingestion-by-context-state';
import { SearchStudioIngestionOperationService } from '../ingestion/search-studio-ingestion-operation-service';
import {
    canResetAllUnindexedOperation,
    canStartAllUnindexedOperation,
    formatOperationProgress,
    hasActiveOperation,
    mapOperationConflictMessage
} from '../ingestion/search-studio-ingestion-operation-state';

@injectable()
export class SearchStudioDocumentWidget extends ReactWidget {

    protected _document?: SearchStudioDocumentOptions;
    protected _ingestionByIdErrorMessage?: string;
    protected _ingestionByIdFetchedPayload?: SearchStudioApiIngestionPayloadEnvelope;
    protected _ingestionByIdIdentifier = '';
    protected _ingestionByIdInfoMessage?: string;
    protected _ingestionContextErrorMessage?: string;
    protected _ingestionContextInfoMessage?: string;
    protected _ingestionContexts: readonly SearchStudioApiIngestionContextResponse[] = [];
    protected _ingestionOperation?: SearchStudioApiIngestionOperationStateResponse;
    protected _ingestionOperationErrorMessage?: string;
    protected _ingestionOperationInfoMessage?: string;
    protected _ingestionSelectedContextValue?: string;
    protected _isFetchingIngestionByIdPayload = false;
    protected _isLoadingIngestionContexts = false;
    protected _isRecoveringIngestionOperation = false;
    protected _isResettingIngestionOperation = false;
    protected _isStartingIngestionOperation = false;
    protected _isSubmittingIngestionByIdPayload = false;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(SearchStudioApiClient)
    protected readonly _apiClient!: SearchStudioApiClient;

    @inject(SearchStudioIngestionOperationService)
    protected readonly _ingestionOperationService!: SearchStudioIngestionOperationService;

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    constructor() {
        super();
        this.addClass('search-studio-document-widget');
    }

    setDocument(document: SearchStudioDocumentOptions): void {
        this._document = document;
        this.resetKindSpecificState(document);
        this.id = document.documentId;
        this.title.label = document.label;
        this.title.caption = document.caption;
        this.title.iconClass = document.iconClass;
        this.title.closable = true;

        if (document.kind === 'ingestion-by-id' || document.kind === 'ingestion-all-unindexed' || document.kind === 'ingestion-by-context') {
            void this.recoverActiveIngestionOperation();
        }

        this.update();
    }

    protected render(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        if (this._document.kind === 'ingestion-by-id') {
            return this.renderIngestionByIdDocument();
        }

        if (this._document.kind === 'ingestion-all-unindexed') {
            return this.renderIngestionAllUnindexedDocument();
        }

        if (this._document.kind === 'ingestion-by-context') {
            return this.renderIngestionByContextDocument();
        }

        return this.renderStandardDocument();
    }

    protected renderDocumentHeader(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        return (
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
        );
    }

    protected renderDocumentMetrics(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        return (
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
        );
    }

    protected renderProviderMetadata(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        return (
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
        );
    }

    protected renderLockedProviderSelector(providerDisplayName: string, providerName: string): React.ReactNode {
        return (
            <label style={{ display: 'grid', gap: '6px', maxWidth: '320px' }}>
                <span style={{ fontWeight: 600 }}>Provider</span>
                <select
                    className="theia-select"
                    value={providerName}
                    disabled
                >
                    <option value={providerName}>{providerDisplayName}</option>
                </select>
            </label>
        );
    }

    protected renderIngestionStatusBanner(message: string | undefined, isError: boolean): React.ReactNode {
        if (!message) {
            return undefined;
        }

        return (
            <div
                style={{
                    padding: '12px',
                    borderRadius: '8px',
                    border: `1px solid ${isError ? 'var(--theia-errorForeground)' : 'var(--theia-successBackground, var(--theia-button-background))'}`,
                    color: isError ? 'var(--theia-errorForeground)' : 'var(--theia-editor-foreground)',
                    background: isError ? 'var(--theia-editorError-background, transparent)' : 'var(--theia-editor-background)'
                }}
            >
                {message}
            </div>
        );
    }

    protected renderStandardDocument(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        return (
            <div style={{ padding: '16px', display: 'grid', gap: '16px', maxWidth: '960px' }}>
                {this.renderDocumentHeader()}
                {this.renderDocumentMetrics()}
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
                {this.renderProviderMetadata()}
                <section style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                    {this._document.actions.map(action => this.renderAction(action))}
                </section>
            </div>
        );
    }

    protected renderIngestionAllUnindexedDocument(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        const viewState = {
            selectedProviderName: this._document.provider.name,
            activeOperation: this._ingestionOperation,
            isRecovering: this._isRecoveringIngestionOperation,
            isStarting: this._isStartingIngestionOperation,
            isResetting: this._isResettingIngestionOperation
        };
        const canStart = canStartAllUnindexedOperation(viewState);
        const canReset = canResetAllUnindexedOperation(viewState);

        return (
            <div style={{ padding: '16px', display: 'grid', gap: '16px', maxWidth: '1080px' }}>
                {this.renderDocumentHeader()}
                {this.renderDocumentMetrics()}
                <section
                    style={{
                        display: 'grid',
                        gap: '12px',
                        padding: '16px',
                        borderRadius: '8px',
                        border: '1px solid var(--theia-panel-border)'
                    }}
                >
                    {this.renderLockedProviderSelector(this._document.provider.displayName, this._document.provider.name)}
                    <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                        <button
                            type="button"
                            className="theia-button"
                            disabled={!canStart}
                            onClick={() => void this.startAllUnindexedIngestion()}
                        >
                            {this._isStartingIngestionOperation ? 'Starting...' : 'Start ingestion'}
                        </button>
                        <button
                            type="button"
                            className="theia-button"
                            style={{
                                background: 'var(--theia-button-background)',
                                color: 'var(--theia-button-foreground)'
                            }}
                            disabled={!canReset}
                            onClick={() => void this.resetAllUnindexedIngestion()}
                        >
                            {this._isResettingIngestionOperation ? 'Resetting...' : 'Reset indexing status'}
                        </button>
                    </div>
                    {this.renderIngestionOperationMessage()}
                    {this.renderIngestionOperationStatus()}
                </section>
                {this.renderProviderMetadata()}
                <section style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                    {this._document.actions.map(action => this.renderAction(action))}
                </section>
            </div>
        );
    }

    protected renderIngestionByContextDocument(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        const viewState = {
            selectedProviderName: this._document.provider.name,
            contexts: this._ingestionContexts,
            selectedContextValue: this._ingestionSelectedContextValue,
            activeOperation: this._ingestionOperation,
            isLoadingContexts: this._isLoadingIngestionContexts,
            isRecovering: this._isRecoveringIngestionOperation,
            isStarting: this._isStartingIngestionOperation,
            isResetting: this._isResettingIngestionOperation
        };
        const canLoad = canLoadContexts(viewState);
        const canStart = canStartContextOperation(viewState);
        const canReset = canResetContextOperation(viewState);

        return (
            <div style={{ padding: '16px', display: 'grid', gap: '16px', maxWidth: '1080px' }}>
                {this.renderDocumentHeader()}
                {this.renderDocumentMetrics()}
                <section
                    style={{
                        display: 'grid',
                        gap: '12px',
                        padding: '16px',
                        borderRadius: '8px',
                        border: '1px solid var(--theia-panel-border)'
                    }}
                >
                    <div style={{ display: 'grid', gap: '6px', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))' }}>
                        {this.renderLockedProviderSelector(this._document.provider.displayName, this._document.provider.name)}
                        <label style={{ display: 'grid', gap: '6px' }}>
                            <span style={{ fontWeight: 600 }}>Context</span>
                            <select
                                className="theia-select"
                                value={this._ingestionSelectedContextValue ?? ''}
                                disabled={this._ingestionContexts.length === 0 || this._isLoadingIngestionContexts}
                                onChange={event => this.handleIngestionContextSelectionChanged(event.currentTarget.value || undefined)}
                            >
                                <option value="">Select a context</option>
                                {this._ingestionContexts.map(context => (
                                    <option key={context.value} value={context.value}>{context.displayName}</option>
                                ))}
                            </select>
                        </label>
                    </div>
                    <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                        <button
                            type="button"
                            className="theia-button"
                            disabled={!canLoad}
                            onClick={() => void this.loadIngestionContexts()}
                        >
                            {this._isLoadingIngestionContexts ? 'Loading contexts...' : 'Load contexts'}
                        </button>
                        <button
                            type="button"
                            className="theia-button"
                            disabled={!canStart}
                            onClick={() => void this.startContextIngestion()}
                        >
                            {this._isStartingIngestionOperation ? 'Starting...' : 'Run context ingestion'}
                        </button>
                        <button
                            type="button"
                            className="theia-button"
                            style={{
                                background: 'var(--theia-button-background)',
                                color: 'var(--theia-button-foreground)'
                            }}
                            disabled={!canReset}
                            onClick={() => void this.resetContextIngestion()}
                        >
                            {this._isResettingIngestionOperation ? 'Resetting...' : 'Reset indexing status'}
                        </button>
                    </div>
                    {this.renderIngestionContextMessage()}
                    {this.renderIngestionOperationMessage()}
                    {this.renderIngestionOperationStatus()}
                </section>
                {this.renderProviderMetadata()}
                <section style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                    {this._document.actions.map(action => this.renderAction(action))}
                </section>
            </div>
        );
    }

    protected renderIngestionByIdDocument(): React.ReactNode {
        if (!this._document) {
            return undefined;
        }

        const viewState = {
            selectedProviderName: this._document.provider.name,
            identifier: this._ingestionByIdIdentifier,
            fetchedPayload: this._ingestionByIdFetchedPayload,
            activeOperation: this._ingestionOperation,
            isRecoveringOperation: this._isRecoveringIngestionOperation,
            isFetching: this._isFetchingIngestionByIdPayload,
            isSubmitting: this._isSubmittingIngestionByIdPayload
        };
        const canFetch = canFetchById(viewState);
        const canSubmit = canSubmitFetchedPayload(viewState);
        const payloadPreview = getPayloadPreviewText(this._ingestionByIdFetchedPayload);

        return (
            <div style={{ padding: '16px', display: 'grid', gap: '16px', maxWidth: '1080px' }}>
                {this.renderDocumentHeader()}
                {this.renderDocumentMetrics()}
                <section
                    style={{
                        display: 'grid',
                        gap: '12px',
                        padding: '16px',
                        borderRadius: '8px',
                        border: '1px solid var(--theia-panel-border)'
                    }}
                >
                    <div style={{ display: 'grid', gap: '6px', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))' }}>
                        {this.renderLockedProviderSelector(this._document.provider.displayName, this._document.provider.name)}
                        <label style={{ display: 'grid', gap: '6px' }}>
                            <span style={{ fontWeight: 600 }}>Id</span>
                            <input
                                className="theia-input"
                                type="text"
                                value={this._ingestionByIdIdentifier}
                                onChange={event => this.handleIngestionByIdIdentifierChanged(event.currentTarget.value)}
                                placeholder="Enter a provider-defined id"
                            />
                        </label>
                    </div>
                    <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                        <button
                            type="button"
                            className="theia-button"
                            disabled={!canFetch}
                            onClick={() => void this.fetchIngestionPayloadById()}
                        >
                            {this._isFetchingIngestionByIdPayload ? 'Fetching...' : 'Fetch'}
                        </button>
                        <button
                            type="button"
                            className="theia-button"
                            style={{
                                background: 'var(--theia-button-background)',
                                color: 'var(--theia-button-foreground)'
                            }}
                            disabled={!canSubmit}
                            onClick={() => void this.submitFetchedIngestionPayload()}
                        >
                            {this._isSubmittingIngestionByIdPayload ? 'Indexing...' : 'Index'}
                        </button>
                    </div>
                    {this.renderIngestionByIdMessage()}
                    {this.renderIngestionOperationMessage()}
                    {this.renderIngestionOperationStatus()}
                </section>
                <section style={{ display: 'grid', gap: '8px' }}>
                    <h2 style={{ margin: 0, fontSize: '1rem' }}>Payload preview</h2>
                    <span style={{ color: 'var(--theia-descriptionForeground)' }}>
                        The fetched payload is shown below in a read-only JSON editor and is reused unchanged for indexing.
                    </span>
                    <textarea
                        readOnly
                        value={payloadPreview}
                        rows={18}
                        spellCheck={false}
                        style={{
                            width: '100%',
                            resize: 'vertical',
                            padding: '12px',
                            borderRadius: '8px',
                            border: '1px solid var(--theia-panel-border)',
                            background: 'var(--theia-editor-background)',
                            color: 'var(--theia-editor-foreground)',
                            fontFamily: 'var(--theia-editor-font-family, monospace)',
                            fontSize: '0.9rem'
                        }}
                    />
                </section>
                {this.renderProviderMetadata()}
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
                if (this._document.kind === 'ingestion-all-unindexed') {
                    await this.resetAllUnindexedIngestion();
                    return;
                }

                if (this._document.kind === 'ingestion-by-context') {
                    await this.resetContextIngestion();
                    return;
                }

                await this._documentService.runResetIngestionStatusPlaceholder(this._document.provider);
                return;
        }
    }

    protected renderIngestionOperationMessage(): React.ReactNode {
        const message = this._ingestionOperationErrorMessage ?? this._ingestionOperationInfoMessage;

        return this.renderIngestionStatusBanner(message, Boolean(this._ingestionOperationErrorMessage));
    }

    protected renderIngestionContextMessage(): React.ReactNode {
        const message = this._ingestionContextErrorMessage ?? this._ingestionContextInfoMessage;

        return this.renderIngestionStatusBanner(message, Boolean(this._ingestionContextErrorMessage));
    }

    protected renderIngestionOperationStatus(): React.ReactNode {
        if (!this._ingestionOperation) {
            return undefined;
        }

        const statusColor = this._ingestionOperation.status === 'failed'
            ? 'var(--theia-errorForeground)'
            : 'var(--theia-descriptionForeground)';

        return (
            <div style={{ display: 'grid', gap: '8px' }}>
                <strong>Operation status</strong>
                <div style={{ display: 'grid', gap: '8px', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))' }}>
                    <div style={{ padding: '12px', borderRadius: '8px', border: '1px solid var(--theia-panel-border)' }}>
                        <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>Operation id</div>
                        <div style={{ marginTop: '6px' }}>{this._ingestionOperation.operationId}</div>
                    </div>
                    <div style={{ padding: '12px', borderRadius: '8px', border: '1px solid var(--theia-panel-border)' }}>
                        <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>Operation type</div>
                        <div style={{ marginTop: '6px' }}>{this._ingestionOperation.operationType}</div>
                    </div>
                    <div style={{ padding: '12px', borderRadius: '8px', border: '1px solid var(--theia-panel-border)' }}>
                        <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>Status</div>
                        <div style={{ marginTop: '6px', color: statusColor }}>{this._ingestionOperation.status}</div>
                    </div>
                    <div style={{ padding: '12px', borderRadius: '8px', border: '1px solid var(--theia-panel-border)' }}>
                        <div style={{ fontSize: '0.85rem', color: 'var(--theia-descriptionForeground)' }}>Progress</div>
                        <div style={{ marginTop: '6px' }}>{formatOperationProgress(this._ingestionOperation)}</div>
                    </div>
                </div>
            </div>
        );
    }

    protected renderIngestionByIdMessage(): React.ReactNode {
        const message = this._ingestionByIdErrorMessage ?? this._ingestionByIdInfoMessage;

        return this.renderIngestionStatusBanner(message, Boolean(this._ingestionByIdErrorMessage));
    }

    protected handleIngestionByIdIdentifierChanged(identifier: string): void {
        this._ingestionByIdIdentifier = identifier;
        this._ingestionByIdErrorMessage = undefined;
        this._ingestionByIdInfoMessage = undefined;
        this.update();
    }

    protected handleIngestionContextSelectionChanged(contextValue: string | undefined): void {
        this._ingestionSelectedContextValue = contextValue;
        this._ingestionContextErrorMessage = undefined;
        this._ingestionContextInfoMessage = contextValue
            ? `Selected context '${getSelectedContextDisplayName(this._ingestionContexts, contextValue) ?? contextValue}'.`
            : undefined;
        this.update();
    }

    protected async fetchIngestionPayloadById(): Promise<void> {
        if (!this._document) {
            return;
        }

        const identifier = this._ingestionByIdIdentifier.trim();

        this._isFetchingIngestionByIdPayload = true;
        this._ingestionByIdErrorMessage = undefined;
        this._ingestionByIdInfoMessage = undefined;
        this.update();

        try {
            const payload = await this._apiClient.getIngestionPayloadById(this._document.provider.name, identifier);

            this._ingestionByIdFetchedPayload = payload;
            this._ingestionByIdInfoMessage = `Loaded payload for id '${payload.id}'.`;
            this._outputService.info(`Loaded payload for id '${payload.id}'.`, 'ingestion');
        } catch (error) {
            this._ingestionByIdFetchedPayload = undefined;
            this._ingestionByIdErrorMessage = mapByIdActionErrorMessage('fetch', identifier, this.getStatusError(error));
            this._outputService.error(this._ingestionByIdErrorMessage, 'ingestion');
        } finally {
            this._isFetchingIngestionByIdPayload = false;
            this.update();
        }
    }

    protected async loadIngestionContexts(): Promise<void> {
        if (!this._document) {
            return;
        }

        this._isLoadingIngestionContexts = true;
        this._ingestionContextErrorMessage = undefined;
        this._ingestionContextInfoMessage = undefined;
        this.update();

        try {
            const response = await this._apiClient.getIngestionContexts(this._document.provider.name);

            this._ingestionContexts = response.contexts;

            if (!this._ingestionSelectedContextValue || !response.contexts.some(context => context.value === this._ingestionSelectedContextValue)) {
                this._ingestionSelectedContextValue = response.contexts.find(context => context.isDefault)?.value ?? response.contexts[0]?.value;
            }

            this._ingestionContextInfoMessage = response.contexts.length === 0
                ? 'No contexts were returned for the selected provider.'
                : `Loaded ${response.contexts.length} context option(s).`;
            this._outputService.info(this._ingestionContextInfoMessage, 'ingestion');
        } catch (error) {
            this._ingestionContexts = [];
            this._ingestionSelectedContextValue = undefined;
            this._ingestionContextErrorMessage = 'Failed to load contexts for the selected provider.';
            this._outputService.error(this._ingestionContextErrorMessage, 'ingestion');
        } finally {
            this._isLoadingIngestionContexts = false;
            this.update();
        }
    }

    protected async startContextIngestion(): Promise<void> {
        if (!this._document || !this._ingestionSelectedContextValue) {
            return;
        }

        this._isStartingIngestionOperation = true;
        this._ingestionOperationErrorMessage = undefined;
        this._ingestionOperationInfoMessage = undefined;
        this.update();

        try {
            const operation = await this._ingestionOperationService.startContextOperation(
                this._document.provider.name,
                this._ingestionSelectedContextValue,
                updatedOperation => this.handleOperationUpdated(updatedOperation));

            this._ingestionOperation = operation;
            this._ingestionOperationInfoMessage = `Started context operation '${operation.operationId}'.`;
            this._outputService.info(this._ingestionOperationInfoMessage, 'ingestion');
        } catch (error) {
            const conflictMessage = error instanceof SearchStudioApiRequestError && error.status === 409
                ? mapOperationConflictMessage(this.getConflictResponse(error))
                : undefined;

            this._ingestionOperationErrorMessage = conflictMessage ?? 'Failed to start context ingestion.';
            this._outputService.error(this._ingestionOperationErrorMessage, 'ingestion');
            await this.recoverActiveIngestionOperation();

            if (conflictMessage) {
                this._ingestionOperationErrorMessage = conflictMessage;
            }
        } finally {
            this._isStartingIngestionOperation = false;
            this.update();
        }
    }

    protected async resetContextIngestion(): Promise<void> {
        if (!this._document || !this._ingestionSelectedContextValue) {
            return;
        }

        this._isResettingIngestionOperation = true;
        this._ingestionOperationErrorMessage = undefined;
        this._ingestionOperationInfoMessage = undefined;
        this.update();

        try {
            const operation = await this._ingestionOperationService.resetIndexingStatusForContextOperation(
                this._document.provider.name,
                this._ingestionSelectedContextValue,
                updatedOperation => this.handleOperationUpdated(updatedOperation));

            this._ingestionOperation = operation;
            this._ingestionOperationInfoMessage = `Started context reset operation '${operation.operationId}'.`;
            this._outputService.info(this._ingestionOperationInfoMessage, 'ingestion');
        } catch (error) {
            const conflictMessage = error instanceof SearchStudioApiRequestError && error.status === 409
                ? mapOperationConflictMessage(this.getConflictResponse(error))
                : undefined;

            this._ingestionOperationErrorMessage = conflictMessage ?? 'Failed to reset indexing status for the selected context.';
            this._outputService.error(this._ingestionOperationErrorMessage, 'ingestion');
            await this.recoverActiveIngestionOperation();

            if (conflictMessage) {
                this._ingestionOperationErrorMessage = conflictMessage;
            }
        } finally {
            this._isResettingIngestionOperation = false;
            this.update();
        }
    }

    protected async submitFetchedIngestionPayload(): Promise<void> {
        if (!this._document) {
            return;
        }

        const request = createSubmitPayloadRequest(this._ingestionByIdFetchedPayload);
        if (!request) {
            return;
        }

        this._isSubmittingIngestionByIdPayload = true;
        this._ingestionByIdErrorMessage = undefined;
        this._ingestionByIdInfoMessage = undefined;
        this.update();

        try {
            const response = await this._apiClient.submitIngestionPayload(this._document.provider.name, request);

            this._ingestionByIdInfoMessage = response.message;
            this._ingestionOperationErrorMessage = undefined;
            this._outputService.info(response.message, 'ingestion');
        } catch (error) {
            if (error instanceof SearchStudioApiRequestError && error.status === 409) {
                const conflictMessage = mapOperationConflictMessage(this.getConflictResponse(error));
                this._ingestionByIdErrorMessage = undefined;
                this._ingestionOperationErrorMessage = conflictMessage;
                this._outputService.error(this._ingestionOperationErrorMessage, 'ingestion');
                await this.recoverActiveIngestionOperation();
                this._ingestionOperationErrorMessage = conflictMessage;
            } else {
                this._ingestionOperationErrorMessage = undefined;
                this._ingestionByIdErrorMessage = mapByIdActionErrorMessage('submit', request.id, this.getStatusError(error));
                this._outputService.error(this._ingestionByIdErrorMessage, 'ingestion');
            }
        } finally {
            this._isSubmittingIngestionByIdPayload = false;
            this.update();
        }
    }

    protected getStatusError(error: unknown): { status?: number } | undefined {
        if (error instanceof SearchStudioApiRequestError) {
            return {
                status: error.status
            };
        }

        return undefined;
    }

    protected getConflictResponse(error: unknown): SearchStudioApiIngestionOperationConflictResponse | undefined {
        if (!(error instanceof SearchStudioApiRequestError) || !error.body) {
            return undefined;
        }

        const body = error.body as Partial<SearchStudioApiIngestionOperationConflictResponse>;
        if (!body.activeOperationId || !body.activeProvider || !body.activeOperationType) {
            return undefined;
        }

        return {
            message: body.message ?? 'Another ingestion operation is already active.',
            activeOperationId: body.activeOperationId,
            activeProvider: body.activeProvider,
            activeOperationType: body.activeOperationType
        };
    }

    protected async recoverActiveIngestionOperation(): Promise<void> {
        if (!this._document || (this._document.kind !== 'ingestion-by-id' && this._document.kind !== 'ingestion-all-unindexed' && this._document.kind !== 'ingestion-by-context')) {
            return;
        }

        this._isRecoveringIngestionOperation = true;
        this._ingestionOperationErrorMessage = undefined;
        this.update();

        try {
            const activeOperation = await this._ingestionOperationService.recoverActiveOperation(operation => this.handleOperationUpdated(operation));
            this._ingestionOperation = activeOperation;

            if (this._document.kind === 'ingestion-by-context' && activeOperation?.context) {
                this._ingestionSelectedContextValue = activeOperation.context;
            }

            this._ingestionOperationInfoMessage = activeOperation && hasActiveOperation(activeOperation)
                ? `Recovered active operation '${activeOperation.operationId}'.`
                : undefined;
        } catch (error) {
            this._ingestionOperationErrorMessage = 'Failed to recover the current ingestion operation.';
            this._outputService.error(this._ingestionOperationErrorMessage, 'ingestion');
        } finally {
            this._isRecoveringIngestionOperation = false;
            this.update();
        }
    }

    protected async startAllUnindexedIngestion(): Promise<void> {
        if (!this._document) {
            return;
        }

        this._isStartingIngestionOperation = true;
        this._ingestionOperationErrorMessage = undefined;
        this._ingestionOperationInfoMessage = undefined;
        this.update();

        try {
            const operation = await this._ingestionOperationService.startAllUnindexedOperation(
                this._document.provider.name,
                updatedOperation => this.handleOperationUpdated(updatedOperation));

            this._ingestionOperation = operation;
            this._ingestionOperationInfoMessage = `Started operation '${operation.operationId}'.`;
            this._outputService.info(this._ingestionOperationInfoMessage, 'ingestion');
        } catch (error) {
            const conflictMessage = error instanceof SearchStudioApiRequestError && error.status === 409
                ? mapOperationConflictMessage(this.getConflictResponse(error))
                : undefined;

            this._ingestionOperationErrorMessage = conflictMessage ?? 'Failed to start ingestion for all unindexed items.';
            this._outputService.error(this._ingestionOperationErrorMessage, 'ingestion');
            await this.recoverActiveIngestionOperation();

            if (conflictMessage) {
                this._ingestionOperationErrorMessage = conflictMessage;
            }
        } finally {
            this._isStartingIngestionOperation = false;
            this.update();
        }
    }

    protected async resetAllUnindexedIngestion(): Promise<void> {
        if (!this._document) {
            return;
        }

        this._isResettingIngestionOperation = true;
        this._ingestionOperationErrorMessage = undefined;
        this._ingestionOperationInfoMessage = undefined;
        this.update();

        try {
            const operation = await this._ingestionOperationService.resetIndexingStatusOperation(
                this._document.provider.name,
                updatedOperation => this.handleOperationUpdated(updatedOperation));

            this._ingestionOperation = operation;
            this._ingestionOperationInfoMessage = `Started reset operation '${operation.operationId}'.`;
            this._outputService.info(this._ingestionOperationInfoMessage, 'ingestion');
        } catch (error) {
            const conflictMessage = error instanceof SearchStudioApiRequestError && error.status === 409
                ? mapOperationConflictMessage(this.getConflictResponse(error))
                : undefined;

            this._ingestionOperationErrorMessage = conflictMessage ?? 'Failed to reset indexing status for all items.';
            this._outputService.error(this._ingestionOperationErrorMessage, 'ingestion');
            await this.recoverActiveIngestionOperation();

            if (conflictMessage) {
                this._ingestionOperationErrorMessage = conflictMessage;
            }
        } finally {
            this._isResettingIngestionOperation = false;
            this.update();
        }
    }

    protected handleOperationUpdated(operation: SearchStudioApiIngestionOperationStateResponse): void {
        this._ingestionOperation = operation;

        if (operation.status === 'failed') {
            this._ingestionOperationErrorMessage = operation.message;
            this._ingestionOperationInfoMessage = undefined;
            this._outputService.error(operation.message, 'ingestion');
        } else if (operation.status === 'succeeded') {
            this._ingestionOperationErrorMessage = undefined;
            this._ingestionOperationInfoMessage = operation.message;
            this._outputService.info(operation.message, 'ingestion');
        } else {
            this._ingestionOperationErrorMessage = undefined;
            this._ingestionOperationInfoMessage = operation.message;
        }

        this.update();
    }

    protected resetKindSpecificState(document: SearchStudioDocumentOptions): void {
        this._ingestionByIdIdentifier = '';
        this._ingestionByIdFetchedPayload = undefined;
        this._ingestionByIdErrorMessage = undefined;
        this._ingestionByIdInfoMessage = undefined;
        this._ingestionContextErrorMessage = undefined;
        this._ingestionContextInfoMessage = undefined;
        this._ingestionContexts = [];
        this._ingestionOperation = undefined;
        this._ingestionOperationErrorMessage = undefined;
        this._ingestionOperationInfoMessage = undefined;
        this._ingestionSelectedContextValue = undefined;
        this._isFetchingIngestionByIdPayload = false;
        this._isLoadingIngestionContexts = false;
        this._isRecoveringIngestionOperation = false;
        this._isResettingIngestionOperation = false;
        this._isStartingIngestionOperation = false;
        this._isSubmittingIngestionByIdPayload = false;

        if (document.kind !== 'ingestion-by-id' && document.kind !== 'ingestion-all-unindexed' && document.kind !== 'ingestion-by-context') {
            return;
        }
    }
}
