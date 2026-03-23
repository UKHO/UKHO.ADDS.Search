import { Emitter } from '@theia/core/lib/common/event';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioOutputService } from '../common/search-studio-output-service';
import { SearchStudioProviderCatalogSnapshot } from '../common/search-studio-shell-types';
import { mapProviderDescriptorsToProviderTreeNodes } from '../providers/search-studio-provider-tree-mapper';
import { SearchStudioApiClient } from './search-studio-api-client';
import { SearchStudioApiProviderDescriptor } from './search-studio-api-types';

@injectable()
export class SearchStudioProviderCatalogService {

    protected readonly _onDidChange = new Emitter<void>();
    protected _loadRequest?: Promise<void>;
    protected _snapshot: SearchStudioProviderCatalogSnapshot = {
        status: 'idle',
        providers: [],
        providerNodes: []
    };

    @inject(SearchStudioApiClient)
    protected readonly _apiClient!: SearchStudioApiClient;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    get onDidChange() {
        return this._onDidChange.event;
    }

    get snapshot(): SearchStudioProviderCatalogSnapshot {
        return this._snapshot;
    }

    async ensureLoaded(): Promise<void> {
        if (this._snapshot.status === 'ready') {
            return;
        }

        await this.refresh();
    }

    async refresh(): Promise<void> {
        if (!this._loadRequest) {
            this._snapshot = {
                status: 'loading',
                providers: this._snapshot.providers,
                providerNodes: this._snapshot.providerNodes
            };
            this._onDidChange.fire();
            this._outputService.info('Loading Studio providers from StudioApiHost /providers.', 'providers');
            this._loadRequest = this.loadProviders();
        }

        await this._loadRequest;
    }

    findProvider(providerName: string): SearchStudioApiProviderDescriptor | undefined {
        return this._snapshot.providers.find(provider => provider.name === providerName);
    }

    protected async loadProviders(): Promise<void> {
        try {
            const providers = await this._apiClient.getProviders();
            this._snapshot = {
                status: 'ready',
                providers,
                providerNodes: mapProviderDescriptorsToProviderTreeNodes(providers)
            };
            this._outputService.info(`Loaded ${providers.length} provider metadata record(s).`, 'providers');
        } catch (error) {
            const errorMessage = error instanceof Error
                ? error.message
                : 'Studio provider metadata could not be loaded.';

            this._snapshot = {
                status: 'error',
                providers: [],
                providerNodes: [],
                errorMessage
            };
            this._outputService.error(errorMessage, 'providers');
        } finally {
            this._loadRequest = undefined;
            this._onDidChange.fire();
        }
    }
}
