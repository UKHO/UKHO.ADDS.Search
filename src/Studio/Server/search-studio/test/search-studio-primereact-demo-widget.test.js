const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

// Provide the minimal browser-like globals that the Theia React widget base class expects when the tests run under Node.
function createElementStub() {
    return {
        style: {},
        classList: { add() {}, remove() {} },
        setAttribute() {},
        removeAttribute() {},
        appendChild() {},
        remove() {},
        addEventListener() {},
        removeEventListener() {},
        nodeType: 1,
        nodeName: 'DIV',
        tagName: 'DIV',
        ownerDocument: global.document
    };
}

global.navigator = { platform: 'Win32', userAgent: 'node.js' };
global.document = {
    createElement: () => createElementStub(),
    documentElement: { style: {} },
    head: { appendChild() {} },
    body: { classList: { add() {}, remove() {} }, addEventListener() {}, removeEventListener() {} },
    getElementById() {
        return null;
    },
    queryCommandSupported() {
        return false;
    },
    addEventListener() {},
    removeEventListener() {}
};
global.window = {
    navigator: global.navigator,
    document: global.document,
    addEventListener() {},
    removeEventListener() {},
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

const { SearchStudioPrimeReactDemoWidget } = require('../lib/browser/primereact-demo/search-studio-primereact-demo-widget.js');

/**
 * Verifies that the PrimeReact demo widget requests an initial render immediately so restored Theia tabs do not reopen blank.
 */
test('SearchStudioPrimeReactDemoWidget requests an initial render during construction', () => {
    let updateCalls = 0;

    class TestSearchStudioPrimeReactDemoWidget extends SearchStudioPrimeReactDemoWidget {
        update() {
            updateCalls += 1;
        }
    }

    const widget = new TestSearchStudioPrimeReactDemoWidget({
        async enableStyledMode() {},
        disableStyledMode() {}
    });

    assert.ok(widget);
    assert.equal(updateCalls, 1);
    assert.equal(widget.title.label, 'PrimeReact Showcase Demo');
    assert.equal(widget.node.style.display, 'flex');
    assert.equal(widget.node.style.height, '100%');
    assert.equal(widget.node.style.overflow, 'hidden');
});

/**
 * Verifies that the PrimeReact demo widget persists and restores the retained showcase page for Theia layout restoration.
 */
test('SearchStudioPrimeReactDemoWidget restores the retained showcase page and falls back from stale page ids', () => {
    class TestSearchStudioPrimeReactDemoWidget extends SearchStudioPrimeReactDemoWidget {
        update() {}
    }

    const widget = new TestSearchStudioPrimeReactDemoWidget({
        async enableStyledMode() {},
        disableStyledMode() {}
    });

    widget.setActiveDemoPage('showcase');
    const storedState = widget.storeState();

    assert.deepEqual(storedState, {
        activeDemoPageId: 'showcase'
    });

    widget.restoreState({
        activeDemoPageId: 'tree'
    });

    assert.equal(widget.title.label, 'PrimeReact Showcase Demo');
    assert.equal(widget.title.caption, 'Temporary PrimeReact combined showcase evaluation demo');
    assert.deepEqual(widget.storeState(), {
        activeDemoPageId: 'showcase'
    });
});

/**
 * Verifies that retired standalone PrimeReact page source files have been removed after their content was consolidated into the showcase tabs.
 */
test('SearchStudioPrimeReactDemoWidget source cleanup removes retired standalone PrimeReact pages', () => {
    const retiredSourceFileNames = [
        'search-studio-primereact-data-table-demo-page.tsx',
        'search-studio-primereact-data-view-demo-page.tsx',
        'search-studio-primereact-forms-demo-page.tsx',
        'search-studio-primereact-layout-demo-page.tsx',
        'search-studio-primereact-tree-demo-page.tsx',
        'search-studio-primereact-tree-table-demo-page.tsx'
    ];
    const retiredTopLevelSourceFileNames = [
        'search-studio-primereact-demo-page.tsx'
    ];

    for (const retiredSourceFileName of retiredSourceFileNames) {
        const retiredSourceFilePath = path.join(__dirname, '..', 'src', 'browser', 'primereact-demo', 'pages', retiredSourceFileName);

        assert.equal(fs.existsSync(retiredSourceFilePath), false, `Expected retired source file ${retiredSourceFileName} to be removed.`);
    }

    for (const retiredTopLevelSourceFileName of retiredTopLevelSourceFileNames) {
        const retiredTopLevelSourceFilePath = path.join(__dirname, '..', 'src', 'browser', 'primereact-demo', retiredTopLevelSourceFileName);

        assert.equal(fs.existsSync(retiredTopLevelSourceFilePath), false, `Expected retired top-level source file ${retiredTopLevelSourceFileName} to be removed.`);
    }
});
