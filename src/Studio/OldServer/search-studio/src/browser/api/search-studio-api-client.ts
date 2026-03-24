import { injectable, inject } from '@theia/core/shared/inversify';
import { SearchStudioApiConfigurationService } from '../search-studio-api-configuration-service';
import { SearchStudioFrontendRequestTimeoutMilliseconds } from '../search-studio-future-api-configuration';
import {
    SearchStudioApiIngestionAcceptedOperationResponse,
    SearchStudioApiIngestionContextsResponse,
    SearchStudioApiIngestionOperationStateResponse,
    SearchStudioApiIngestionPayloadEnvelope,
    SearchStudioApiIngestionOperationConflictResponse,
    SearchStudioApiIngestionSubmitPayloadRequest,
    SearchStudioApiIngestionSubmitPayloadResponse,
    SearchStudioApiProviderDescriptor,
    SearchStudioApiRuleDiscoveryResponse
} from './search-studio-api-types';

export class SearchStudioApiRequestError extends Error {
    constructor(
        readonly status: number,
        readonly statusText: string,
        message: string,
        readonly body?: SearchStudioApiIngestionOperationConflictResponse | Record<string, unknown>
    ) {
        super(message);
        this.name = 'SearchStudioApiRequestError';
    }
}

@injectable()
export class SearchStudioApiClient {

    @inject(SearchStudioApiConfigurationService)
    protected readonly _apiConfigurationService!: SearchStudioApiConfigurationService;

    async getProviders(): Promise<readonly SearchStudioApiProviderDescriptor[]> {
        return this.getJson<readonly SearchStudioApiProviderDescriptor[]>('/providers');
    }

    async getRules(): Promise<SearchStudioApiRuleDiscoveryResponse> {
        return this.getJson<SearchStudioApiRuleDiscoveryResponse>('/rules');
    }

    async getIngestionPayloadById(providerName: string, identifier: string): Promise<SearchStudioApiIngestionPayloadEnvelope> {
        return this.getJson<SearchStudioApiIngestionPayloadEnvelope>(`/ingestion/${encodeURIComponent(providerName)}/${encodeURIComponent(identifier)}`);
    }

    async submitIngestionPayload(
        providerName: string,
        request: SearchStudioApiIngestionSubmitPayloadRequest
    ): Promise<SearchStudioApiIngestionSubmitPayloadResponse>
    {
        return this.sendJson<SearchStudioApiIngestionSubmitPayloadRequest, SearchStudioApiIngestionSubmitPayloadResponse>(
            `/ingestion/${encodeURIComponent(providerName)}/payload`,
            'POST',
            request);
    }

    async getIngestionContexts(providerName: string): Promise<SearchStudioApiIngestionContextsResponse> {
        return this.getJson<SearchStudioApiIngestionContextsResponse>(`/ingestion/${encodeURIComponent(providerName)}/contexts`);
    }

    async startIngestionAllUnindexed(providerName: string): Promise<SearchStudioApiIngestionAcceptedOperationResponse> {
        return this.requestJson<SearchStudioApiIngestionAcceptedOperationResponse>(`/ingestion/${encodeURIComponent(providerName)}/all`, 'PUT');
    }

    async startIngestionContext(providerName: string, context: string): Promise<SearchStudioApiIngestionAcceptedOperationResponse> {
        return this.requestJson<SearchStudioApiIngestionAcceptedOperationResponse>(`/ingestion/${encodeURIComponent(providerName)}/context/${encodeURIComponent(context)}`, 'PUT');
    }

    async resetIngestionIndexingStatus(providerName: string): Promise<SearchStudioApiIngestionAcceptedOperationResponse> {
        return this.requestJson<SearchStudioApiIngestionAcceptedOperationResponse>(`/ingestion/${encodeURIComponent(providerName)}/operations/reset-indexing-status`, 'POST');
    }

    async resetIngestionIndexingStatusForContext(providerName: string, context: string): Promise<SearchStudioApiIngestionAcceptedOperationResponse> {
        return this.requestJson<SearchStudioApiIngestionAcceptedOperationResponse>(`/ingestion/${encodeURIComponent(providerName)}/context/${encodeURIComponent(context)}/operations/reset-indexing-status`, 'POST');
    }

    async getActiveOperation(): Promise<SearchStudioApiIngestionOperationStateResponse | undefined> {
        try {
            return await this.getJson<SearchStudioApiIngestionOperationStateResponse>('/operations/active');
        } catch (error) {
            if (error instanceof SearchStudioApiRequestError && error.status === 404) {
                return undefined;
            }

            throw error;
        }
    }

    async getOperation(operationId: string): Promise<SearchStudioApiIngestionOperationStateResponse> {
        return this.getJson<SearchStudioApiIngestionOperationStateResponse>(`/operations/${encodeURIComponent(operationId)}`);
    }

    async getOperationEventsUrl(operationId: string): Promise<string> {
        return this.getRequestUrl(`/operations/${encodeURIComponent(operationId)}/events`);
    }

    protected async getJson<T>(path: string): Promise<T> {
        return this.requestJson<T>(path, 'GET');
    }

    protected async sendJson<TRequest, TResponse>(path: string, method: 'POST' | 'PUT', body: TRequest): Promise<TResponse> {
        return this.requestJson<TResponse>(path, method, body);
    }

    protected async requestJson<T>(path: string, method: 'GET' | 'POST' | 'PUT', body?: unknown): Promise<T> {
        const requestUrl = await this.getRequestUrl(path);
        const abortController = new AbortController();
        const timeout = window.setTimeout(() => abortController.abort(), SearchStudioFrontendRequestTimeoutMilliseconds);

        try {
            const response = await fetch(requestUrl, {
                method,
                headers: {
                    Accept: 'application/json',
                    ...(body === undefined ? {} : { 'Content-Type': 'application/json' })
                },
                body: body === undefined ? undefined : JSON.stringify(body),
                signal: abortController.signal
            });

            if (!response.ok) {
                const responseBody = await this.tryReadErrorBody(response);

                throw new SearchStudioApiRequestError(
                    response.status,
                    response.statusText,
                    `StudioApiHost request failed: ${response.status} ${response.statusText}`,
                    responseBody);
            }

            return await response.json() as T;
        } catch (error) {
            if (error instanceof Error && error.name === 'AbortError') {
                throw new Error(`StudioApiHost request timed out after ${SearchStudioFrontendRequestTimeoutMilliseconds}ms.`);
            }

            throw error;
        } finally {
            window.clearTimeout(timeout);
        }
    }

    protected async getRequestUrl(path: string): Promise<string> {
        const studioApiHostBaseUrl = (await this._apiConfigurationService.getConfiguration()).studioApiHostBaseUrl;

        if (!studioApiHostBaseUrl) {
            throw new Error('StudioApiHost base URL is not configured for the studio shell.');
        }

        return new URL(path, `${studioApiHostBaseUrl}/`).toString();
    }

    protected async tryReadErrorBody(response: Response): Promise<Record<string, unknown> | undefined> {
        const contentType = response.headers.get('content-type') ?? '';

        if (!contentType.includes('application/json')) {
            return undefined;
        }

        try {
            return await response.json() as Record<string, unknown>;
        } catch {
            return undefined;
        }
    }
}
