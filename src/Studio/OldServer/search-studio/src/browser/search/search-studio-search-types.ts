export interface SearchStudioSearchRequestedEvent {
    readonly query: string;
}

export interface SearchStudioMockSearchResult {
    readonly id: string;
    readonly title: string;
    readonly type: string;
    readonly region: string;
    readonly source: string;
    readonly summary: string;
}

export interface SearchStudioMockFacetOption {
    readonly id: string;
    readonly label: string;
    readonly count: number;
}

export interface SearchStudioMockFacetGroup {
    readonly id: string;
    readonly label: string;
    readonly options: readonly SearchStudioMockFacetOption[];
}
