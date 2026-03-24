const test = require('node:test');
const assert = require('node:assert/strict');
const { SearchStudioIngestionToolbarContribution } = require('../lib/browser/ingestion/search-studio-ingestion-toolbar-contribution.js');
const {
    SearchStudioIngestionWidgetId,
    SearchStudioRefreshProvidersCommand
} = require('../lib/browser/search-studio-constants.js');

test('SearchStudioIngestionToolbarContribution registers the native refresh action for Ingestion only', () => {
    const contribution = new SearchStudioIngestionToolbarContribution();
    const items = [];

    contribution.registerToolbarItems({
        registerItem: item => {
            items.push(item);
            return { dispose() {} };
        }
    });

    assert.deepEqual(
        items.map(item => ({ id: item.id, command: item.command, tooltip: item.tooltip })),
        [
            {
                id: 'search-studio.ingestion.refresh.toolbar',
                command: SearchStudioRefreshProvidersCommand.id,
                tooltip: 'Refresh Providers'
            }
        ]);

    const ingestionWidget = { id: SearchStudioIngestionWidgetId };

    assert.equal(items.every(item => item.isVisible(ingestionWidget)), true);
    assert.equal(items.every(item => item.isVisible({})), false);
});
