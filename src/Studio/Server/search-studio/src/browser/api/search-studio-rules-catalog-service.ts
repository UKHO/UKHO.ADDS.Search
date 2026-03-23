import { Emitter } from '@theia/core/lib/common/event';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioOutputService } from '../common/search-studio-output-service';
import { mapRuleDiscoveryResponseToRulesSnapshot } from '../rules/search-studio-rules-mapper';
import { SearchStudioRulesCatalogSnapshot, SearchStudioRulesProviderGroup } from '../rules/search-studio-rules-types';
import { SearchStudioApiClient } from './search-studio-api-client';

@injectable()
export class SearchStudioRulesCatalogService {

    protected readonly _onDidChange = new Emitter<void>();
    protected _loadRequest?: Promise<void>;
    protected _snapshot: SearchStudioRulesCatalogSnapshot = {
        status: 'idle',
        providers: []
    };

    @inject(SearchStudioApiClient)
    protected readonly _apiClient!: SearchStudioApiClient;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    get onDidChange() {
        return this._onDidChange.event;
    }

    get snapshot(): SearchStudioRulesCatalogSnapshot {
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
                schemaVersion: this._snapshot.schemaVersion,
                providers: this._snapshot.providers
            };
            this._onDidChange.fire();
            this._outputService.info('Loading Studio rules from StudioApiHost /rules.', 'rules');
            this._loadRequest = this.loadRules();
        }

        await this._loadRequest;
    }

    findProviderGroup(providerName: string): SearchStudioRulesProviderGroup | undefined {
        return this._snapshot.providers.find(group => group.provider.name === providerName);
    }

    protected async loadRules(): Promise<void> {
        try {
            const response = await this._apiClient.getRules();
            this._snapshot = mapRuleDiscoveryResponseToRulesSnapshot(response);
            this._outputService.info(`Loaded rules for ${this._snapshot.providers.length} provider(s).`, 'rules');
        } catch (error) {
            const errorMessage = error instanceof Error
                ? error.message
                : 'Studio rule discovery could not be loaded.';

            this._snapshot = {
                status: 'error',
                providers: [],
                errorMessage
            };
            this._outputService.error(errorMessage, 'rules');
        } finally {
            this._loadRequest = undefined;
            this._onDidChange.fire();
        }
    }
}
