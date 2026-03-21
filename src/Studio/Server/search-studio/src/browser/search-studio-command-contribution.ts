import { inject, injectable } from '@theia/core/shared/inversify';
import { CommandContribution, CommandRegistry, MessageService } from '@theia/core/lib/common';
import { SearchStudioGreetingCommand } from './search-studio-constants';

@injectable()
export class SearchStudioCommandContribution implements CommandContribution {

    @inject(MessageService)
    protected readonly messageService!: MessageService;

    registerCommands(registry: CommandRegistry): void {
        registry.registerCommand(SearchStudioGreetingCommand, {
            execute: () => this.messageService.info('UKHO Search Studio is ready.')
        });
    }
}
