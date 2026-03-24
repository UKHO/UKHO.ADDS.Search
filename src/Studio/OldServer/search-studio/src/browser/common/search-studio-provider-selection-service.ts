import { Emitter } from '@theia/core/lib/common/event';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioApiProviderDescriptor } from '../api/search-studio-api-types';
import { SearchStudioOutputService } from './search-studio-output-service';
import { SearchStudioWorkArea } from './search-studio-shell-types';
import { resolvePreferredProvider } from './search-studio-provider-resolution';

@injectable()
export class SearchStudioProviderSelectionService {

    protected readonly _onDidChangeSelectedProvider = new Emitter<string | undefined>();
    protected _selectedProviderName?: string;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    get onDidChangeSelectedProvider() {
        return this._onDidChangeSelectedProvider.event;
    }

    get selectedProviderName(): string | undefined {
        return this._selectedProviderName;
    }

    selectProvider(provider: SearchStudioApiProviderDescriptor, workArea: SearchStudioWorkArea): void {
        if (this._selectedProviderName === provider.name) {
            return;
        }

        this._selectedProviderName = provider.name;
        this._outputService.info(`Selected provider '${provider.name}' from ${workArea}.`, 'provider-selection');
        this._onDidChangeSelectedProvider.fire(this._selectedProviderName);
    }

    synchronizeProviderSelection(
        providers: readonly SearchStudioApiProviderDescriptor[],
        workArea: SearchStudioWorkArea
    ): void
    {
        const provider = resolvePreferredProvider(providers, this._selectedProviderName);

        if (!provider) {
            return;
        }

        this.selectProvider(provider, workArea);
    }
}
