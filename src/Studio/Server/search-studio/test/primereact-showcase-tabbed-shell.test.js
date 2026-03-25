const assert = require('node:assert/strict');
const test = require('node:test');
const React = require('@theia/core/shared/react');
const ReactDOMServer = require('react-dom/server');
const {
    SearchStudioPrimeReactShowcaseDemoPage,
    SearchStudioPrimeReactShowcaseTabOrder,
    createSearchStudioPrimeReactShowcaseInitialRenderState,
    activateSearchStudioPrimeReactShowcaseRenderState,
    focusSearchStudioPrimeReactShowcaseTabContent,
    getSearchStudioPrimeReactShowcaseTabFocusTargetId
} = require('../lib/browser/primereact-demo/pages/search-studio-primereact-showcase-demo-page');

// Verify the consolidated showcase keeps the fixed final tab order required by the implementation plan.
test('SearchStudioPrimeReactShowcaseTabOrder_WhenRead_ShouldMatchTheFixedRetainedTabOrder', () => {
    assert.deepStrictEqual(SearchStudioPrimeReactShowcaseTabOrder, [
        'showcase',
        'forms',
        'dataview',
        'datatable',
        'tree',
        'treetable'
    ]);
});

// Verify the root tab shell opens on Showcase and leaves the other tabs lazy until reviewers activate them.
test('createSearchStudioPrimeReactShowcaseInitialRenderState_WhenCalled_ShouldRenderOnlyTheDefaultShowcaseTab', () => {
    assert.deepStrictEqual(createSearchStudioPrimeReactShowcaseInitialRenderState(), {
        showcase: true,
        forms: false,
        dataview: false,
        datatable: false,
        tree: false,
        treetable: false
    });
});

// Verify tab activation keeps previously rendered tabs mounted so local tab state can survive tab switches.
test('activateSearchStudioPrimeReactShowcaseRenderState_WhenTabsAreActivated_ShouldPreservePreviouslyRenderedTabs', () => {
    const initialState = createSearchStudioPrimeReactShowcaseInitialRenderState();
    const formsActivatedState = activateSearchStudioPrimeReactShowcaseRenderState(initialState, 'forms');
    const dataViewActivatedState = activateSearchStudioPrimeReactShowcaseRenderState(formsActivatedState, 'dataview');

    assert.deepStrictEqual(formsActivatedState, {
        showcase: true,
        forms: true,
        dataview: false,
        datatable: false,
        tree: false,
        treetable: false
    });
    assert.deepStrictEqual(dataViewActivatedState, {
        showcase: true,
        forms: true,
        dataview: true,
        datatable: false,
        tree: false,
        treetable: false
    });
});

// Verify the rendered root tab shell exposes the consolidated labels in order and renders Showcase content by default.
test('SearchStudioPrimeReactShowcaseDemoPage_WhenRendered_ShouldExposeTheRootTabsAndOnlyTheDefaultShowcaseContentInitially', () => {
    const markup = ReactDOMServer.renderToStaticMarkup(
        React.createElement(SearchStudioPrimeReactShowcaseDemoPage, {
            activeThemeVariant: 'light'
        })
    );
    const labelsInOrder = ['Showcase', 'Forms', 'Data View', 'Data Table', 'Tree', 'Tree Table'];
    let previousLabelIndex = -1;

    for (const label of labelsInOrder) {
        const currentLabelIndex = markup.indexOf(label);

        assert.notStrictEqual(currentLabelIndex, -1, `Expected tab label ${label} to be present in the rendered markup.`);
        assert.ok(currentLabelIndex > previousLabelIndex, `Expected tab label ${label} to appear after the previous tab label.`);
        previousLabelIndex = currentLabelIndex;
    }

    assert.match(markup, /Compact review workspace/);
    assert.doesNotMatch(markup, /Controlled form inputs/);
    assert.doesNotMatch(markup, /Card and list controls/);
    assert.doesNotMatch(markup, /tab coming next/);
});

// Verify the consolidated shell can resolve a stable focus target for each retained tab so keyboard focus can move into the content after a tab switch.
test('getSearchStudioPrimeReactShowcaseTabFocusTargetId_WhenCalled_ShouldReturnStableContentTargetIdentifiers', () => {
    assert.equal(
        getSearchStudioPrimeReactShowcaseTabFocusTargetId('showcase'),
        'search-studio-primereact-showcase-tab-focus-target-showcase'
    );
    assert.equal(
        getSearchStudioPrimeReactShowcaseTabFocusTargetId('treetable'),
        'search-studio-primereact-showcase-tab-focus-target-treetable'
    );
});

// Verify the browser-side focus helper moves keyboard focus into the requested tab content when the target exists.
test('focusSearchStudioPrimeReactShowcaseTabContent_WhenTargetExists_ShouldMoveKeyboardFocusIntoTheRequestedTabContent', () => {
    const originalDocument = global.document;
    const expectedTargetId = getSearchStudioPrimeReactShowcaseTabFocusTargetId('tree');
    const fakeDocument = {
        activeElement: null,
        getElementById(targetId) {
            if (targetId !== expectedTargetId) {
                return null;
            }

            return {
                focus() {
                    fakeDocument.activeElement = this;
                }
            };
        }
    };

    global.document = fakeDocument;

    try {
        assert.equal(focusSearchStudioPrimeReactShowcaseTabContent('tree'), true);
        assert.notEqual(fakeDocument.activeElement, null);
    } finally {
        global.document = originalDocument;
    }
});
