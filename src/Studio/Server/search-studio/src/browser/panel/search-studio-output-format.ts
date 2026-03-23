import { SearchStudioOutputEntry } from '../common/search-studio-shell-types';

const outputSeverityLabels: Record<SearchStudioOutputEntry['level'], string> = {
    info: 'INFO',
    error: 'ERROR'
};

const outputSeverityColors: Record<SearchStudioOutputEntry['level'], string> = {
    info: 'var(--search-studio-output-info-severity, #A9C7FF)',
    error: 'var(--search-studio-output-error-severity, #FFB3BA)'
};

export function formatOutputTimestamp(timestamp: string): string {
    const date = new Date(timestamp);

    if (Number.isNaN(date.getTime())) {
        return timestamp;
    }

    return date.toISOString().slice(11, 19);
}

export function formatOutputSeverity(level: SearchStudioOutputEntry['level']): string {
    return outputSeverityLabels[level];
}

export function getOutputSeverityColor(level: SearchStudioOutputEntry['level']): string {
    return outputSeverityColors[level];
}

export function formatOutputEntryText(entry: SearchStudioOutputEntry): string {
    return [
        formatOutputTimestamp(entry.timestamp),
        formatOutputSeverity(entry.level),
        entry.source,
        entry.message
    ].join(' ');
}

export function getRevealLatestScrollPosition(entryCount: number, scrollHeight: number): number {
    return entryCount === 0 ? 0 : scrollHeight;
}
