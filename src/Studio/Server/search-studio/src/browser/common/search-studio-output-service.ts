import { Emitter } from '@theia/core/lib/common/event';
import { injectable } from '@theia/core/shared/inversify';
import { SearchStudioOutputEntry } from './search-studio-shell-types';

@injectable()
export class SearchStudioOutputService {

    protected readonly _entries: SearchStudioOutputEntry[] = [];
    protected readonly _onDidChangeEntries = new Emitter<void>();
    protected _entrySequence = 0;

    get onDidChangeEntries() {
        return this._onDidChangeEntries.event;
    }

    get entries(): readonly SearchStudioOutputEntry[] {
        return this._entries;
    }

    info(message: string, source = 'studio-shell'): void {
        this.append('info', message, source);
    }

    error(message: string, source = 'studio-shell'): void {
        this.append('error', message, source);
    }

    clear(): void {
        this._entries.length = 0;
        this._onDidChangeEntries.fire();
    }

    protected append(level: 'info' | 'error', message: string, source: string): void {
        this._entrySequence += 1;
        this._entries.unshift({
            id: `entry-${this._entrySequence}`,
            timestamp: new Date().toISOString(),
            level,
            source,
            message
        });

        if (this._entries.length > 200) {
            this._entries.length = 200;
        }

        this._onDidChangeEntries.fire();
    }
}
