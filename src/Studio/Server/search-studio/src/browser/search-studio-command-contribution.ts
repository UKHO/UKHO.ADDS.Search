import { CommandContribution, CommandRegistry } from '@theia/core/lib/common';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioHomeService } from './home/search-studio-home-service';
import { SearchStudioShowHomeCommand } from './search-studio-home-constants';

/**
 * Registers the minimal Studio commands needed to reopen the Home document from standard Theia surfaces.
 */
@injectable()
export class SearchStudioCommandContribution implements CommandContribution {

    /**
     * Creates the Studio command contribution.
     *
     * @param homeService Reopens the shared Home document widget when the user invokes the Home command.
     */
    constructor(
        @inject(SearchStudioHomeService)
        protected readonly homeService: SearchStudioHomeService
    ) {
        // The command contribution currently needs only the shared Home service dependency.
    }

    /**
     * Registers the Studio Home command with the command registry.
     *
     * @param registry Stores the command definition and execution handler.
     */
    registerCommands(registry: CommandRegistry): void {
        // Keep the legacy command identity and label so the View menu can continue exposing Home with the expected wording.
        registry.registerCommand(SearchStudioShowHomeCommand, {
            execute: async () => {
                await this.homeService.openHome();
            }
        });
    }
}
