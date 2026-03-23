import { injectable } from '@theia/core/shared/inversify';
import { AbstractViewContribution } from '@theia/core/lib/browser/shell/view-contribution';
import {
    SearchStudioIngestionViewContainerId,
    SearchStudioIngestionToggleCommandId,
    SearchStudioIngestionWidgetId,
    SearchStudioIngestionWidgetLabel
} from '../search-studio-constants';
import { SearchStudioIngestionWidget } from './search-studio-ingestion-widget';

@injectable()
export class SearchStudioIngestionViewContribution extends AbstractViewContribution<SearchStudioIngestionWidget> {

    constructor() {
        super({
            widgetId: SearchStudioIngestionWidgetId,
            viewContainerId: SearchStudioIngestionViewContainerId,
            widgetName: SearchStudioIngestionWidgetLabel,
            defaultWidgetOptions: {
                area: 'left'
            },
            toggleCommandId: SearchStudioIngestionToggleCommandId
        });
    }
}
