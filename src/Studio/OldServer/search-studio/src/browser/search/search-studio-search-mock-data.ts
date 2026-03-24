import {
    SearchStudioMockFacetGroup,
    SearchStudioMockSearchResult
} from './search-studio-search-types';

export const SearchStudioMockFacetGroups: readonly SearchStudioMockFacetGroup[] = [
    {
        id: 'region',
        label: 'Region',
        options: [
            { id: 'north-sea', label: 'North Sea', count: 54 },
            { id: 'baltic', label: 'Baltic', count: 12 },
            { id: 'atlantic', label: 'Atlantic', count: 8 }
        ]
    },
    {
        id: 'type',
        label: 'Type',
        options: [
            { id: 'wreck', label: 'Wreck', count: 39 },
            { id: 'pipeline', label: 'Pipeline', count: 22 },
            { id: 'cable', label: 'Cable', count: 17 }
        ]
    }
];

export const SearchStudioMockSearchResults: readonly SearchStudioMockSearchResult[] = [
    {
        id: 'result-001',
        title: 'Wreck - North Sea - Example 001',
        type: 'Wreck',
        region: 'North Sea',
        source: 'Mock Hydrographic Dataset',
        summary: 'Illustrative mock detail content for a wreck result in the North Sea.'
    },
    {
        id: 'result-002',
        title: 'Wreck - North Sea - Example 002',
        type: 'Wreck',
        region: 'North Sea',
        source: 'Mock Hydrographic Dataset',
        summary: 'Illustrative mock detail content for an additional wreck result in the North Sea.'
    },
    {
        id: 'result-003',
        title: 'Pipeline - Norway - Example 003',
        type: 'Pipeline',
        region: 'Atlantic',
        source: 'Mock Hydrographic Dataset',
        summary: 'Illustrative mock detail content for a pipeline result associated with Norway.'
    },
    {
        id: 'result-004',
        title: 'Cable - Baltic - Example 004',
        type: 'Cable',
        region: 'Baltic',
        source: 'Mock Hydrographic Dataset',
        summary: 'Illustrative mock detail content for a cable result in the Baltic region.'
    },
    {
        id: 'result-005',
        title: 'Pipeline - North Sea - Example 005',
        type: 'Pipeline',
        region: 'North Sea',
        source: 'Mock Hydrographic Dataset',
        summary: 'Illustrative mock detail content for a pipeline result in the North Sea.'
    }
];
