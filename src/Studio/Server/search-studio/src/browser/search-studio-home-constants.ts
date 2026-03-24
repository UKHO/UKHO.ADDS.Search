import { Command } from '@theia/core/lib/common';

/**
 * Identifies the Studio Home widget instance in the workbench.
 */
export const SearchStudioHomeWidgetId = 'search-studio.home';

/**
 * Identifies the widget factory that recreates the Studio Home document tab.
 */
export const SearchStudioHomeWidgetFactoryId = 'search-studio.home';

/**
 * Supplies the visible tab label used for the restored Studio Home document.
 */
export const SearchStudioHomeWidgetLabel = 'Home';

/**
 * Supplies the workbench icon class used for the Studio Home document tab.
 */
export const SearchStudioHomeWidgetIconClass = 'codicon codicon-home';

/**
 * Reopens the Studio Home document from commands and menus.
 */
export const SearchStudioShowHomeCommand: Command = {
    id: 'search-studio.home.show',
    category: 'UKHO Search Studio',
    label: 'Home'
};
