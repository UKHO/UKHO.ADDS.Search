import * as React from '@theia/core/shared/react';
import { inject, injectable } from '@theia/core/shared/inversify';
import { CommandRegistry } from '@theia/core/lib/common';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { SearchStudioGreetingCommand, SearchStudioWidgetId, SearchStudioWidgetLabel } from './search-studio-constants';

@injectable()
export class SearchStudioWidget extends ReactWidget {

    @inject(CommandRegistry)
    protected readonly commandRegistry!: CommandRegistry;

    constructor() {
        super();
        this.id = SearchStudioWidgetId;
        this.title.label = SearchStudioWidgetLabel;
        this.title.caption = 'Welcome to UKHO Search Studio';
        this.title.closable = true;
        this.addClass('search-studio-welcome-widget');
        this.update();
    }

    protected render(): React.ReactNode {
        return (
            <div style={{ padding: '16px', display: 'grid', gap: '12px', maxWidth: '720px' }}>
                <h1 style={{ margin: 0 }}>{SearchStudioWidgetLabel}</h1>
                <p style={{ margin: 0 }}>
                    Welcome to the first Eclipse Theia shell for UKHO Search. This starter view keeps the standard workbench intact
                    while proving that the custom native Theia extension is active.
                </p>
                <p style={{ margin: 0 }}>
                    This work package intentionally stays lightweight and does not migrate existing tooling workflows into the shell.
                </p>
                <div>
                    <button
                        type="button"
                        className="theia-button"
                        onClick={() => this.commandRegistry.executeCommand(SearchStudioGreetingCommand.id)}
                    >
                        {SearchStudioGreetingCommand.label}
                    </button>
                </div>
            </div>
        );
    }
}
