const test = require('node:test');
const assert = require('node:assert/strict');
const {
    formatOutputEntryText,
    formatOutputSeverity,
    formatOutputTimestamp,
    getOutputSeverityColor,
    getRevealLatestScrollPosition
} = require('../lib/browser/panel/search-studio-output-format.js');

test('formatOutputTimestamp trims ISO timestamps to hh:mm:ss', () => {
    assert.equal(formatOutputTimestamp('2026-03-23T10:03:37.280Z'), '10:03:37');
});

test('formatOutputTimestamp returns the original value for invalid timestamps', () => {
    assert.equal(formatOutputTimestamp('not-a-date'), 'not-a-date');
});

test('formatOutputSeverity returns stable uppercase tokens', () => {
    assert.equal(formatOutputSeverity('info'), 'INFO');
    assert.equal(formatOutputSeverity('error'), 'ERROR');
});

test('getOutputSeverityColor returns the agreed pastel token colors', () => {
    assert.equal(getOutputSeverityColor('info'), 'var(--search-studio-output-info-severity, #A9C7FF)');
    assert.equal(getOutputSeverityColor('error'), 'var(--search-studio-output-error-severity, #FFB3BA)');
});

test('formatOutputEntryText preserves merged-stream line ordering and source metadata', () => {
    assert.equal(
        formatOutputEntryText({
            id: 'entry-7',
            timestamp: '2026-03-23T10:03:37.280Z',
            level: 'info',
            source: 'providers',
            message: 'Loaded provider metadata.'
        }),
        '10:03:37 INFO providers Loaded provider metadata.'
    );
});

test('getRevealLatestScrollPosition scrolls to the latest line when output exists', () => {
    assert.equal(getRevealLatestScrollPosition(3, 480), 480);
});

test('getRevealLatestScrollPosition resets to the top for an empty output pane', () => {
    assert.equal(getRevealLatestScrollPosition(0, 480), 0);
});
