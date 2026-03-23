/**
 * Generated using theia-extension-generator
 */
import { FrontendApplicationContribution, WidgetFactory, bindViewContribution } from '@theia/core/lib/browser';
import { CommandContribution, MenuContribution } from '@theia/core/lib/common';
import { ContainerModule } from '@theia/core/shared/inversify';
import { SearchStudioApiClient } from './api/search-studio-api-client';
import { SearchStudioProviderCatalogService } from './api/search-studio-provider-catalog-service';
import { SearchStudioRulesCatalogService } from './api/search-studio-rules-catalog-service';
import { SearchStudioDocumentService } from './common/search-studio-document-service';
import { SearchStudioDocumentWidget } from './common/search-studio-document-widget';
import { SearchStudioOutputService } from './common/search-studio-output-service';
import { SearchStudioProviderSelectionService } from './common/search-studio-provider-selection-service';
import { SearchStudioDocumentOptions } from './common/search-studio-shell-types';
import { SearchStudioIngestionViewContainerFactory } from './ingestion/search-studio-ingestion-view-container-factory';
import { SearchStudioIngestionViewContribution } from './ingestion/search-studio-ingestion-view-contribution';
import { SearchStudioIngestionWidget } from './ingestion/search-studio-ingestion-widget';
import { SearchStudioOutputViewContribution } from './panel/search-studio-output-view-contribution';
import { SearchStudioOutputWidget } from './panel/search-studio-output-widget';
import { SearchStudioProvidersViewContainerFactory } from './providers/search-studio-providers-view-container-factory';
import { SearchStudioRulesViewContainerFactory } from './rules/search-studio-rules-view-container-factory';
import { SearchStudioRulesViewContribution } from './rules/search-studio-rules-view-contribution';
import { SearchStudioRulesWidget } from './rules/search-studio-rules-widget';
import { SearchStudioApiConfigurationService } from './search-studio-api-configuration-service';
import { SearchStudioCommandContribution } from './search-studio-command-contribution';
import { SearchStudioShellLayoutContribution } from './search-studio-shell-layout-contribution';
import {
    SearchStudioDocumentWidgetFactoryId,
    SearchStudioIngestionWidgetId,
    SearchStudioOutputWidgetId,
    SearchStudioRulesWidgetId,
    SearchStudioWidgetId
} from './search-studio-constants';
import { SearchStudioMenuContribution } from './search-studio-menu-contribution';
import { SearchStudioViewContribution } from './search-studio-view-contribution';
import { SearchStudioWidget } from './search-studio-widget';

export default new ContainerModule(bind => {
    bind(CommandContribution).to(SearchStudioCommandContribution);
    bind(MenuContribution).to(SearchStudioMenuContribution);
    bind(SearchStudioApiConfigurationService).toSelf().inSingletonScope();
    bind(SearchStudioApiClient).toSelf().inSingletonScope();
    bind(SearchStudioProviderCatalogService).toSelf().inSingletonScope();
    bind(SearchStudioRulesCatalogService).toSelf().inSingletonScope();
    bind(SearchStudioProviderSelectionService).toSelf().inSingletonScope();
    bind(SearchStudioOutputService).toSelf().inSingletonScope();
    bind(SearchStudioDocumentService).toSelf().inSingletonScope();
    bind(SearchStudioWidget).toSelf();
    bind(SearchStudioRulesWidget).toSelf();
    bind(SearchStudioIngestionWidget).toSelf();
    bind(SearchStudioOutputWidget).toSelf();
    bind(SearchStudioDocumentWidget).toSelf();
    bind(SearchStudioProvidersViewContainerFactory).toSelf().inSingletonScope();
    bind(SearchStudioRulesViewContainerFactory).toSelf().inSingletonScope();
    bind(SearchStudioIngestionViewContainerFactory).toSelf().inSingletonScope();
    bind(SearchStudioShellLayoutContribution).toSelf().inSingletonScope();
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioWidgetId,
        createWidget: () => context.container.get<SearchStudioWidget>(SearchStudioWidget)
    }));
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioRulesWidgetId,
        createWidget: () => context.container.get<SearchStudioRulesWidget>(SearchStudioRulesWidget)
    }));
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioIngestionWidgetId,
        createWidget: () => context.container.get<SearchStudioIngestionWidget>(SearchStudioIngestionWidget)
    }));
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioOutputWidgetId,
        createWidget: () => context.container.get<SearchStudioOutputWidget>(SearchStudioOutputWidget)
    }));
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioDocumentWidgetFactoryId,
        createWidget: (options?: unknown) => {
            const widget = context.container.get<SearchStudioDocumentWidget>(SearchStudioDocumentWidget);
            widget.setDocument(options as SearchStudioDocumentOptions);
            return widget;
        }
    }));
    bind(WidgetFactory).toDynamicValue(context => context.container.get<SearchStudioProvidersViewContainerFactory>(SearchStudioProvidersViewContainerFactory));
    bind(WidgetFactory).toDynamicValue(context => context.container.get<SearchStudioRulesViewContainerFactory>(SearchStudioRulesViewContainerFactory));
    bind(WidgetFactory).toDynamicValue(context => context.container.get<SearchStudioIngestionViewContainerFactory>(SearchStudioIngestionViewContainerFactory));
    bindViewContribution(bind, SearchStudioViewContribution);
    bindViewContribution(bind, SearchStudioRulesViewContribution);
    bindViewContribution(bind, SearchStudioIngestionViewContribution);
    bindViewContribution(bind, SearchStudioOutputViewContribution);
    bind(FrontendApplicationContribution).toService(SearchStudioShellLayoutContribution);
});
