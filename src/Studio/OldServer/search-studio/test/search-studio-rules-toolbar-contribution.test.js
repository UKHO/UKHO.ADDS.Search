const test = require('node:test');
const assert = require('node:assert/strict');
const { SearchStudioRulesToolbarContribution } = require('../lib/browser/rules/search-studio-rules-toolbar-contribution.js');
const {
    SearchStudioNewRuleCommand,
    SearchStudioRefreshRulesCommand,
    SearchStudioRulesWidgetId
} = require('../lib/browser/search-studio-constants.js');

test('SearchStudioRulesToolbarContribution registers native toolbar actions for Rules only', () => {
    const contribution = new SearchStudioRulesToolbarContribution();
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
                id: 'search-studio.rules.newRule.toolbar',
                command: SearchStudioNewRuleCommand.id,
                tooltip: 'New Rule'
            },
            {
                id: 'search-studio.rules.refresh.toolbar',
                command: SearchStudioRefreshRulesCommand.id,
                tooltip: 'Refresh Rules'
            }
        ]);

    const rulesWidget = { id: SearchStudioRulesWidgetId };

    assert.equal(items.every(item => item.isVisible(rulesWidget)), true);
    assert.equal(items.every(item => item.isVisible({})), false);
});
