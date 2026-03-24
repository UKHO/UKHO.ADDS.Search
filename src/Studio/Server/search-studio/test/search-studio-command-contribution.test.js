const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that Theia command imports expect when the tests run under Node.
global.navigator = { platform: 'Win32', userAgent: 'node.js' };
global.document = {
    createElement: () => ({ style: {}, classList: { add() {}, remove() {} }, setAttribute() {}, removeAttribute() {}, nodeType: 1, ownerDocument: null }),
    documentElement: { style: {} },
    body: { classList: { add() {}, remove() {} } },
    queryCommandSupported() {
        return false;
    },
    addEventListener() {},
    removeEventListener() {}
};
global.window = {
    navigator: global.navigator,
    document: global.document,
    localStorage: {
        getItem() {
            return undefined;
        },
        setItem() {},
        removeItem() {}
    }
};
global.HTMLElement = class HTMLElement {};
global.Element = global.HTMLElement;
global.DragEvent = class DragEvent {};
require.extensions['.css'] = () => {};

const {
    SearchStudioCommandContribution
} = require('../lib/browser/search-studio-command-contribution.js');
const {
    SearchStudioShowHomeCommand
} = require('../lib/browser/search-studio-home-constants.js');

/**
 * Verifies that the registered Home command reuses the shared Home service reopen behavior.
 */
test('SearchStudioCommandContribution opens Home from the registered Show Home command', async () => {
    const registeredCommands = new Map();
    let openHomeCalls = 0;

    // Register the command against a lightweight fake registry so the test can invoke the exact handler that Theia would store.
    const contribution = new SearchStudioCommandContribution({
        async openHome() {
            openHomeCalls += 1;
        }
    });

    contribution.registerCommands({
        registerCommand: (command, handler) => {
            registeredCommands.set(command.id, handler);
        }
    });

    await registeredCommands.get(SearchStudioShowHomeCommand.id).execute();

    assert.equal(openHomeCalls, 1);
});
