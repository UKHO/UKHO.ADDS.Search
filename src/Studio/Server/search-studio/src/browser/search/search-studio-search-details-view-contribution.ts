import { injectable } from '@theia/core/shared/inversify';
import { AbstractViewContribution } from '@theia/core/lib/browser/shell/view-contribution';
import {
    SearchStudioSearchDetailsToggleCommandId,
    SearchStudioSearchDetailsWidgetId,
    SearchStudioSearchDetailsWidgetLabel
} from '../search-studio-constants';
import { SearchStudioSearchDetailsWidget } from './search-studio-search-details-widget';

@injectable()
export class SearchStudioSearchDetailsViewContribution extends AbstractViewContribution<SearchStudioSearchDetailsWidget> {

    constructor() {
        super({
            widgetId: SearchStudioSearchDetailsWidgetId,
            widgetName: SearchStudioSearchDetailsWidgetLabel,
            defaultWidgetOptions: {
                area: 'right'
            },
            toggleCommandId: SearchStudioSearchDetailsToggleCommandId
        });
    }
}
