import { injectable } from '@theia/core/shared/inversify';
import { MenuContribution, MenuModelRegistry } from '@theia/core/lib/common';
import { CommonMenus } from '@theia/core/lib/browser';
import { SearchStudioGreetingCommand } from './search-studio-constants';

@injectable()
export class SearchStudioMenuContribution implements MenuContribution {

    registerMenus(menus: MenuModelRegistry): void {
        menus.registerMenuAction(CommonMenus.HELP, {
            commandId: SearchStudioGreetingCommand.id,
            label: SearchStudioGreetingCommand.label
        });
    }
}
