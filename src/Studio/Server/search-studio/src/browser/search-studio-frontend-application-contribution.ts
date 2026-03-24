import { FrontendApplication, FrontendApplicationContribution } from '@theia/core/lib/browser';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioHomeService } from './home/search-studio-home-service';
import { SearchStudioRuntimeConfigurationService } from './search-studio-runtime-configuration-service';

/**
 * Preloads the Studio runtime configuration during frontend startup and opens Home once the Theia layout is ready.
 */
@injectable()
export class SearchStudioFrontendApplicationContribution implements FrontendApplicationContribution {

    /**
     * Creates the frontend startup contribution.
     *
     * @param runtimeConfigurationService Loads the same-origin runtime configuration bridge payload.
     * @param homeService Opens the Studio Home document after Theia finishes preparing the workbench layout.
     */
    constructor(
        @inject(SearchStudioRuntimeConfigurationService)
        protected readonly runtimeConfigurationService: SearchStudioRuntimeConfigurationService,
        @inject(SearchStudioHomeService)
        protected readonly homeService: SearchStudioHomeService
    ) {
        // The contribution only needs the runtime configuration and Home services for startup validation and default-open behavior.
    }

    /**
     * Preloads the runtime bridge after the frontend application starts.
     *
     * @param _application The frontend application instance that triggered startup.
     * @returns A promise that completes after configuration validation logging has finished.
     */
    async onStart(_application: FrontendApplication): Promise<void> {
        try {
            // Resolve the runtime configuration eagerly so shell startup logs immediately show the active Studio API base URL.
            const configuration = await this.runtimeConfigurationService.getConfiguration();

            if (configuration.studioApiHostBaseUrl) {
                console.info('Studio runtime configuration resolved.', configuration);
            } else {
                // Warn instead of failing startup so the generated Theia shell still opens for diagnostics.
                console.warn('Studio runtime configuration resolved without a Studio API base URL.', configuration);
            }
        } catch (error) {
            // Keep startup running, but log the failure so developers can diagnose the broken configuration bridge.
            console.error('Failed to resolve Studio runtime configuration during startup.', error);
        }
    }

    /**
     * Opens the Studio Home document after the workbench layout is ready to accept main-area widgets.
     *
     * @param _application The frontend application instance that finished initializing its layout.
     * @returns A promise that completes after the Home document has been opened or its failure has been logged.
     */
    async initializeLayout(_application: FrontendApplication): Promise<void> {
        try {
            // Open Home only after Theia finishes creating the layout so the main area can attach the tab successfully.
            await this.homeService.openHome();
        } catch (error) {
            // Keep startup running when Home fails so the generated Theia shell remains available for diagnostics.
            console.error('Failed to open Studio Home during layout initialization.', error);
        }
    }
}
