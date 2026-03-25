import { SearchStudioPrimeReactDemoThemeVariant } from './search-studio-primereact-demo-presentation-state';

/**
 * Identifies how a temporary PrimeReact page is currently hosted inside the Studio shell.
 */
export type SearchStudioPrimeReactDemoHostDisplayMode = 'standalone' | 'tabbed';

/**
 * Describes the immutable configuration supplied by the hosting widget to each temporary PrimeReact demo page.
 */
export interface SearchStudioPrimeReactDemoPageProps {
    /**
     * Identifies the active stock PrimeReact theme variant that matches the current Theia theme.
     */
    readonly activeThemeVariant: SearchStudioPrimeReactDemoThemeVariant;

    /**
     * Identifies whether the page is being shown as its own Theia page or inside the consolidated showcase tab shell.
     */
    readonly hostDisplayMode?: SearchStudioPrimeReactDemoHostDisplayMode;
}
