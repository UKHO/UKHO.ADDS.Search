import { injectable } from '@theia/core/shared/inversify';
import { AbstractViewContribution } from '@theia/core/lib/browser/shell/view-contribution';
import {
    SearchStudioOutputToggleCommandId,
    SearchStudioOutputWidgetId,
    SearchStudioOutputWidgetLabel
} from '../search-studio-constants';
import { SearchStudioOutputWidget } from './search-studio-output-widget';

@injectable()
export class SearchStudioOutputViewContribution extends AbstractViewContribution<SearchStudioOutputWidget> {

    constructor() {
        super({
            widgetId: SearchStudioOutputWidgetId,
            widgetName: SearchStudioOutputWidgetLabel,
            defaultWidgetOptions: {
                area: 'bottom'
            },
            toggleCommandId: SearchStudioOutputToggleCommandId
        });
    }
}
