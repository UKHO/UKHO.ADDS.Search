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
const {
    SearchStudioShowPrimeReactShowcaseDemoCommand
} = require('../lib/browser/primereact-demo/search-studio-primereact-demo-constants.js');

/**
 * Creates a lightweight command contribution plus tracking state for the PrimeReact demo command tests.
 *
 * @returns The fake contribution dependencies plus the registered command map.
 */
function createPrimeReactCommandTestContext() {
    const registeredCommands = new Map();
    let openHomeCalls = 0;
    const openedDemoPages = [];

    // Register the commands against lightweight fake services so each test can invoke the exact handler stored by Theia.
    const contribution = new SearchStudioCommandContribution({
        async openHome() {
            openHomeCalls += 1;
        }
    }, {
        async openDemo(pageId) {
            openedDemoPages.push(pageId);
        }
    });

    contribution.registerCommands({
        registerCommand: (command, handler) => {
            registeredCommands.set(command.id, handler);
        }
    });

    return {
        registeredCommands,
        getOpenHomeCalls() {
            return openHomeCalls;
        },
        openedDemoPages
    };
}

/**
 * Verifies that the registered Home command reuses the shared Home service reopen behavior.
 */
test('SearchStudioCommandContribution opens Home from the registered Show Home command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowHomeCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 1);
    assert.deepEqual(commandTestContext.openedDemoPages, []);
});

/**
 * Verifies that the command contribution keeps only the consolidated showcase command for the PrimeReact research surface.
 */
test('SearchStudioCommandContribution registers only the consolidated PrimeReact showcase command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    assert.equal(commandTestContext.registeredCommands.size, 2);

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactShowcaseDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['showcase']);
});
