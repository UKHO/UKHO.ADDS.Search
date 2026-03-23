import { injectable } from '@theia/core/shared/inversify';
import { AbstractViewContribution } from '@theia/core/lib/browser/shell/view-contribution';
import {
    SearchStudioProvidersViewContainerId,
    SearchStudioToggleCommandId,
    SearchStudioWidgetId,
    SearchStudioWidgetLabel
} from './search-studio-constants';
import { SearchStudioWidget } from './search-studio-widget';

@injectable()
export class SearchStudioViewContribution extends AbstractViewContribution<SearchStudioWidget> {

    constructor() {
        super({
            widgetId: SearchStudioWidgetId,
            viewContainerId: SearchStudioProvidersViewContainerId,
            widgetName: SearchStudioWidgetLabel,
            defaultWidgetOptions: {
                area: 'left'
            },
            toggleCommandId: SearchStudioToggleCommandId
        });
    }
}
