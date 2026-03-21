import { injectable } from '@theia/core/shared/inversify';
import { FrontendApplication, FrontendApplicationContribution } from '@theia/core/lib/browser';
import { AbstractViewContribution } from '@theia/core/lib/browser/shell/view-contribution';
import { SearchStudioToggleCommandId, SearchStudioWidgetId, SearchStudioWidgetLabel } from './search-studio-constants';
import { SearchStudioWidget } from './search-studio-widget';

@injectable()
export class SearchStudioViewContribution extends AbstractViewContribution<SearchStudioWidget> implements FrontendApplicationContribution {

    constructor() {
        super({
            widgetId: SearchStudioWidgetId,
            widgetName: SearchStudioWidgetLabel,
            defaultWidgetOptions: {
                area: 'main'
            },
            toggleCommandId: SearchStudioToggleCommandId
        });
    }

    async initializeLayout(_app: FrontendApplication): Promise<void> {
        await this.openView({
            activate: false,
            reveal: true
        });
    }
}
