import { injectable } from '@theia/core/shared/inversify';
import { SearchStudioPrimeReactDemoThemeVariant } from './search-studio-primereact-demo-presentation-state';
import { getSearchStudioPrimeReactGeneratedThemeDefinition } from '../primereact-theme/generated/search-studio-generated-primereact-theme-content';

const PrimeReactVersion = '10.9.7';
const PrimeIconsVersion = '7.0.0';
const PrimeReactCoreStylesheetId = 'search-studio-primereact-core-stylesheet';
const PrimeReactThemeStylesheetId = 'search-studio-primereact-theme-stylesheet';
const PrimeIconsStylesheetId = 'search-studio-primereact-icons-stylesheet';
const TemporaryDemoStylesheetAttribute = 'data-search-studio-temporary';
const GeneratedThemeFileNameAttribute = 'data-search-studio-generated-theme-file-name';
const PrimeReactCoreStylesheetUrl = `https://cdn.jsdelivr.net/npm/primereact@${PrimeReactVersion}/resources/primereact.min.css`;
const PrimeIconsStylesheetUrl = `https://cdn.jsdelivr.net/npm/primeicons@${PrimeIconsVersion}/primeicons.css`;

/**
 * Manages the temporary PrimeReact stylesheets used by the research demo page.
 */
@injectable()
export class SearchStudioPrimeReactDemoThemeService {

    /**
     * Enables PrimeReact styled mode by attaching the shared PrimeReact stylesheets and the generated UKHO/Theia theme content.
     *
     * @param themeVariant Identifies which generated UKHO/Theia light or dark theme should match the active Theia theme.
     * @returns A promise that completes when the generated UKHO/Theia theme content has been applied.
     */
    async enableStyledMode(themeVariant: SearchStudioPrimeReactDemoThemeVariant): Promise<void> {
        const headElement = document.head;

        if (!headElement) {
        // Fail loudly when the browser head is unavailable because the temporary research page cannot attach PrimeReact styles without it.
            throw new Error('PrimeReact styled mode cannot be enabled because the document head is unavailable.');
        }

        // Ensure the shared PrimeReact core styles are present before loading the theme-specific stylesheet.
        this.ensureStylesheetLink(headElement, PrimeReactCoreStylesheetId, PrimeReactCoreStylesheetUrl);

        // Ensure the icon stylesheet is present so PrimeReact button/icon affordances render correctly during styled-mode review.
        this.ensureStylesheetLink(headElement, PrimeIconsStylesheetId, PrimeIconsStylesheetUrl);

        // Swap the generated UKHO/Theia theme stylesheet content to the variant that matches the current Theia light or dark workbench theme.
        await this.ensureGeneratedThemeStylesheet(headElement, themeVariant);
        console.info('Enabled PrimeReact styled mode for the temporary demo using the generated UKHO/Theia theme.', { themeVariant });
    }

    /**
     * Disables PrimeReact styled mode by removing the temporary PrimeReact stylesheets from the document head.
     */
    disableStyledMode(): void {
        // Remove the generated theme stylesheet first so the temporary page immediately stops applying the UKHO/Theia theme layer.
        this.removeStylesheet(PrimeReactThemeStylesheetId);

        // Remove the shared PrimeReact core stylesheet so no styled-mode rules leak into the rest of the shell after the demo is closed or toggled off.
        this.removeStylesheet(PrimeReactCoreStylesheetId);

        // Remove the icon stylesheet as part of the same cleanup because the temporary demo is the only page currently using it.
        this.removeStylesheet(PrimeIconsStylesheetId);
        console.info('Disabled PrimeReact styled mode for the temporary demo.');
    }

    /**
     * Resolves the generated UKHO/Theia theme definition that matches the current Theia theme variant.
     *
     * @param themeVariant Identifies whether the temporary demo should use the generated light or dark UKHO/Theia theme.
     * @returns The generated theme definition for the requested light or dark variant.
     */
    protected getThemeStylesheetDefinition(themeVariant: SearchStudioPrimeReactDemoThemeVariant) {
        // Resolve the generated theme definition from one shared generated module so runtime theme switching stays aligned with the deploy workflow output.
        return getSearchStudioPrimeReactGeneratedThemeDefinition(themeVariant);
    }

    /**
     * Creates or updates a stylesheet link element in the current document head.
     *
     * @param headElement Supplies the document head that hosts the temporary stylesheet links.
     * @param stylesheetId Identifies the stylesheet link element so it can be updated or removed later.
     * @param stylesheetUrl Supplies the stylesheet URL that should be applied to the link element.
     * @returns A promise that completes once the stylesheet link has been ensured.
     */
    protected ensureStylesheetLink(
        headElement: HTMLHeadElement,
        stylesheetId: string,
        stylesheetUrl: string
    ): Promise<void> {
        const existingStylesheet = document.getElementById(stylesheetId) as HTMLLinkElement | null;

        if (existingStylesheet) {
            // When the stylesheet is already present with the same URL, avoid unnecessary DOM churn and network work.
            if (existingStylesheet.href === stylesheetUrl) {
                return Promise.resolve();
            }

            // Repoint the existing temporary stylesheet link so the browser can keep the shared PrimeReact assets aligned without adding duplicate nodes.
            existingStylesheet.href = stylesheetUrl;
            return Promise.resolve();
        }

        // Create the temporary stylesheet link lazily so the shell does not carry PrimeReact styled assets until the demo actually needs them.
        const stylesheet = document.createElement('link');
        stylesheet.id = stylesheetId;
        stylesheet.rel = 'stylesheet';
        stylesheet.setAttribute(TemporaryDemoStylesheetAttribute, 'true');
        headElement.appendChild(stylesheet);

        // Apply the URL after appending so the shared PrimeReact stylesheet starts loading only when the temporary demo needs it.
        stylesheet.href = stylesheetUrl;
        return Promise.resolve();
    }

    /**
     * Creates or updates the generated UKHO/Theia theme stylesheet element in the current document head.
     *
     * @param headElement Supplies the document head that hosts the temporary generated theme stylesheet.
     * @param themeVariant Identifies which generated UKHO/Theia theme variant should be applied.
     * @returns A promise that completes once the generated theme stylesheet content has been updated.
     */
    protected ensureGeneratedThemeStylesheet(
        headElement: HTMLHeadElement,
        themeVariant: SearchStudioPrimeReactDemoThemeVariant
    ): Promise<void> {
        const themeDefinition = this.getThemeStylesheetDefinition(themeVariant);
        const existingStylesheet = document.getElementById(PrimeReactThemeStylesheetId) as HTMLStyleElement | null;

        if (existingStylesheet) {
            // Update the existing generated theme node in place so variant changes do not accumulate multiple light and dark theme layers.
            existingStylesheet.textContent = themeDefinition.stylesheetContent;
            existingStylesheet.setAttribute(GeneratedThemeFileNameAttribute, themeDefinition.fileName);
            return Promise.resolve();
        }

        // Create the generated theme style element lazily so the shell only carries the UKHO/Theia theme layer while the temporary demo is open.
        const stylesheet = document.createElement('style');
        stylesheet.id = PrimeReactThemeStylesheetId;
        stylesheet.setAttribute(TemporaryDemoStylesheetAttribute, 'true');
        stylesheet.setAttribute(GeneratedThemeFileNameAttribute, themeDefinition.fileName);
        stylesheet.textContent = themeDefinition.stylesheetContent;
        headElement.appendChild(stylesheet);
        return Promise.resolve();
    }

    /**
     * Removes a temporary PrimeReact stylesheet element when it exists.
     *
     * @param stylesheetId Identifies the stylesheet element that should be removed from the document head.
     */
    protected removeStylesheet(stylesheetId: string): void {
        const stylesheet = document.getElementById(stylesheetId);

        if (!stylesheet) {
            // Ignore missing stylesheets because the temporary demo may be cleaning up after a failed or partial attach sequence.
            return;
        }

        // Remove the existing temporary stylesheet so the shell returns to its normal non-PrimeReact state when styled mode is off.
        stylesheet.remove();
    }
}
