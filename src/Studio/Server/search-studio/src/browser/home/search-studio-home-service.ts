import { ApplicationShell } from '@theia/core/lib/browser';
import { WidgetManager } from '@theia/core/lib/browser/widget-manager';
import { inject, injectable } from '@theia/core/shared/inversify';
import { Widget } from '@lumino/widgets';
import { SearchStudioHomeWidget } from './search-studio-home-widget';
import { SearchStudioHomeWidgetFactoryId } from '../search-studio-home-constants';

/**
 * Opens and reactivates the Studio Home document so startup and menu actions share the same workbench behavior.
 */
@injectable()
export class SearchStudioHomeService {

    /**
     * Creates the Home document service.
     *
     * @param shell Supplies access to the Theia main area where the Home document tab is hosted.
     * @param widgetManager Reuses the single Home widget instance across startup and View menu reopen actions.
     */
    constructor(
        @inject(ApplicationShell)
        protected readonly shell: ApplicationShell,
        @inject(WidgetManager)
        protected readonly widgetManager: WidgetManager
    ) {
        // The service only stores core workbench dependencies so every Home open path behaves consistently.
    }

    /**
     * Opens the Studio Home document in the main workbench area and activates the tab.
     *
     * @returns A promise that completes after the Home widget is attached and focused.
     */
    async openHome(): Promise<void> {
        try {
            // Reuse the same widget instance so closing and reopening Home preserves normal Theia document behavior.
            const widget = await this.widgetManager.getOrCreateWidget<SearchStudioHomeWidget>(SearchStudioHomeWidgetFactoryId);

            if (!widget.isAttached) {
                // Insert Home before the first main-area widget when possible so it feels like the initial landing document.
                const firstMainAreaWidget = this.getFirstMainAreaWidget();
                await this.shell.addWidget(widget, {
                    area: 'main',
                    mode: firstMainAreaWidget ? 'tab-before' : undefined,
                    ref: firstMainAreaWidget
                });
            }

            // Always reactivate the Home tab so View menu reopen behaves the same as startup-open.
            await this.shell.activateWidget(widget.id);
            console.info('Opened Studio Home.');
        } catch (error) {
            // Surface failures in the browser console so document attachment problems remain diagnosable during startup and manual smoke tests.
            console.error('Failed to open Studio Home.', error);
            throw error;
        }
    }

    /**
     * Gets the first existing main-area widget so Home can be inserted as a normal document tab before it.
     *
     * @returns The first widget in the main workbench area when one exists; otherwise, `undefined`.
     */
    protected getFirstMainAreaWidget(): Widget | undefined {
        // Stop after the first widget because the service only needs a single tab reference for the Home insertion point.
        for (const widget of this.shell.mainPanel.widgets()) {
            return widget;
        }

        return undefined;
    }
}
