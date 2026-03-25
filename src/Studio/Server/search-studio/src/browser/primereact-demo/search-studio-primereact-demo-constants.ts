import { Command } from '@theia/core/lib/common';

/**
 * Identifies the single retained temporary PrimeReact demo page that can be opened from the Theia `View` menu.
 */
export type SearchStudioPrimeReactDemoPageId = 'showcase';

/**
 * Describes the metadata used to expose one temporary PrimeReact demo page through commands, menus, and widget titles.
 */
export interface SearchStudioPrimeReactDemoPageDefinition {
    /**
     * Stores the stable logical page identifier used by the temporary demo widget.
     */
    readonly pageId: SearchStudioPrimeReactDemoPageId;

    /**
     * Stores the visible command and menu label shown to reviewers.
     */
    readonly label: string;

    /**
     * Stores the visible document tab label shown when the widget renders the page.
     */
    readonly widgetLabel: string;

    /**
     * Stores the explanatory document-tab caption shown for the page.
     */
    readonly widgetCaption: string;
}

/**
 * Describes one temporary PrimeReact page entry that should be exposed through Theia commands and menus.
 */
export interface SearchStudioPrimeReactDemoCommandDefinition {
    /**
     * Stores the stable logical page identifier that should open when the command runs.
     */
    readonly pageId: SearchStudioPrimeReactDemoPageId;

    /**
     * Stores the Theia command object registered for the temporary demo page.
     */
    readonly command: Command;

    /**
     * Stores the visible View-menu label shown to reviewers.
     */
    readonly menuLabel: string;
}

/**
 * Identifies the temporary PrimeReact demo widget instance in the workbench.
 */
export const SearchStudioPrimeReactDemoWidgetId = 'search-studio.primereact-demo';

/**
 * Identifies the widget factory that recreates the temporary PrimeReact demo document.
 */
export const SearchStudioPrimeReactDemoWidgetFactoryId = 'search-studio.primereact-demo';

/**
 * Supplies the visible tab label used for the temporary PrimeReact research document.
 */
export const SearchStudioPrimeReactDemoWidgetLabel = 'PrimeReact Showcase Demo';

/**
 * Supplies the workbench icon class used for the temporary PrimeReact research document tab.
 */
export const SearchStudioPrimeReactDemoWidgetIconClass = 'codicon codicon-symbol-color';

/**
 * Opens the temporary PrimeReact combined showcase research page from commands and menus.
 */
export const SearchStudioShowPrimeReactShowcaseDemoCommand: Command = {
    id: 'search-studio.primereact-demo.showcase.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Showcase Demo'
};

const searchStudioPrimeReactDemoPageDefinitions: Record<SearchStudioPrimeReactDemoPageId, SearchStudioPrimeReactDemoPageDefinition> = {
    showcase: {
        pageId: 'showcase',
        label: 'PrimeReact Showcase Demo',
        widgetLabel: 'PrimeReact Showcase Demo',
        widgetCaption: 'Temporary PrimeReact combined showcase evaluation demo'
    }
};

const searchStudioPrimeReactDemoCommandDefinitions: ReadonlyArray<SearchStudioPrimeReactDemoCommandDefinition> = [
    {
        pageId: 'showcase',
        command: SearchStudioShowPrimeReactShowcaseDemoCommand,
        menuLabel: 'PrimeReact Showcase Demo'
    }
];

/**
 * Gets the metadata definition for a supported temporary PrimeReact demo page.
 *
 * @param pageId Identifies the logical page that should be shown in the shared temporary demo widget.
 * @returns The metadata definition for the requested page.
 */
export function getSearchStudioPrimeReactDemoPageDefinition(
    pageId: SearchStudioPrimeReactDemoPageId
): SearchStudioPrimeReactDemoPageDefinition {
    // Resolve the shared page metadata from one central dictionary so commands, menus, and the widget stay aligned.
    return searchStudioPrimeReactDemoPageDefinitions[pageId];
}

/**
 * Gets the ordered temporary PrimeReact command definitions used by the shared command and menu contributions.
 *
 * @returns The immutable ordered list of temporary PrimeReact command definitions.
 */
export function getSearchStudioPrimeReactDemoCommandDefinitions(): ReadonlyArray<SearchStudioPrimeReactDemoCommandDefinition> {
    // Keep the temporary demo registrations in one ordered list so the entire research surface remains easy to review and remove later.
    return searchStudioPrimeReactDemoCommandDefinitions;
}
