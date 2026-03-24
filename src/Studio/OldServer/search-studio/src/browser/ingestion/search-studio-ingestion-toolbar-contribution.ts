import { Widget } from '@theia/core/lib/browser';
import { TabBarToolbarContribution, TabBarToolbarRegistry } from '@theia/core/lib/browser/shell/tab-bar-toolbar/tab-bar-toolbar-registry';
import { injectable } from '@theia/core/shared/inversify';
import {
    SearchStudioIngestionWidgetId,
    SearchStudioRefreshProvidersCommand
} from '../search-studio-constants';

@injectable()
export class SearchStudioIngestionToolbarContribution implements TabBarToolbarContribution {

    registerToolbarItems(registry: TabBarToolbarRegistry): void {
        registry.registerItem({
            id: 'search-studio.ingestion.refresh.toolbar',
            command: SearchStudioRefreshProvidersCommand.id,
            icon: 'codicon codicon-refresh',
            tooltip: 'Refresh Providers',
            group: 'navigation',
            priority: 0,
            isVisible: widget => this.isIngestionWidget(widget)
        });
    }

    protected isIngestionWidget(widget?: Widget): boolean {
        return widget?.id === SearchStudioIngestionWidgetId;
    }
}
