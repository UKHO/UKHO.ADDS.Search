import { Widget } from '@theia/core/lib/browser';
import { TabBarToolbarContribution, TabBarToolbarRegistry } from '@theia/core/lib/browser/shell/tab-bar-toolbar/tab-bar-toolbar-registry';
import { injectable } from '@theia/core/shared/inversify';
import {
    SearchStudioNewRuleCommand,
    SearchStudioRefreshRulesCommand,
    SearchStudioRulesWidgetId
} from '../search-studio-constants';

@injectable()
export class SearchStudioRulesToolbarContribution implements TabBarToolbarContribution {

    registerToolbarItems(registry: TabBarToolbarRegistry): void {
        registry.registerItem({
            id: 'search-studio.rules.newRule.toolbar',
            command: SearchStudioNewRuleCommand.id,
            icon: 'codicon codicon-add',
            tooltip: 'New Rule',
            group: 'navigation',
            priority: 0,
            isVisible: widget => this.isRulesWidget(widget)
        });

        registry.registerItem({
            id: 'search-studio.rules.refresh.toolbar',
            command: SearchStudioRefreshRulesCommand.id,
            icon: 'codicon codicon-refresh',
            tooltip: 'Refresh Rules',
            group: 'navigation',
            priority: 1,
            isVisible: widget => this.isRulesWidget(widget)
        });
    }

    protected isRulesWidget(widget?: Widget): boolean {
        return widget?.id === SearchStudioRulesWidgetId;
    }
}
