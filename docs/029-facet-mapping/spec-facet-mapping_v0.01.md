# Specification: Facet mapping (nested key/value for discovery) (v0.01)

## 1. Overview

### 1.1 Purpose
Define how the search index and query layer will support returning all facet names and their value counts for a given search query in a single response, independent of paging through hits.

### 1.2 Scope
In scope:
- Change the Elasticsearch index representation of facets from dynamic fields (`facets.<name>`) to a `nested` array of key/value entries.
- Define the Elasticsearch aggregation/query shape required to return discovered facet names plus value buckets and counts.
- Define high-level impacts on query API responses and the Blazor UI facet panel.

Out of scope:
- Implementing the index migration/reindex.
- Defining the final UX for facet ordering, grouping, or value truncation.
- Provider-specific facet governance (beyond supporting heterogeneous providers).

### 1.3 Stakeholders
- Search UI (Blazor) consumers
- Query Service owners
- Ingestion/indexing owners
- Data providers producing facet metadata

### 1.4 Definitions
- Facet name: the logical filter category (e.g., `provider`, `statusCode`).
- Facet value: a value within a facet category (e.g., `ukho`, `A1`).
- Discovery: ability to determine facet names present in the matched result set at query time.
- Paging: returning only a subset of hits via `from/size` or `search_after`.

## 2. System context

### 2.1 Current state
- Canonical documents emit a facet bag as `SortedDictionary<string, SortedSet<string>> Facets`.
- Elasticsearch mapping supports dynamic facet keys under `facets.*` mapped as `keyword`.
- Aggregations can be computed for known facet fields, but facet names cannot be reliably discovered at query time without prior knowledge of the set of facet names.
- The UI risks missing facet categories if it infers available facets from paged hits.

### 2.2 Proposed state
- Index facets as a `nested` collection of `name`/`value` entries:
  - `facets: [{ "name": "<facetName>", "value": "<facetValue>" }]`
- Query Elasticsearch using a single `nested` aggregation that:
  1) terms-aggregates on `facets.name` to discover facet names present in the matched result set
  2) within each name bucket, terms-aggregates on `facets.value` to return value buckets and counts
- The Query Service returns these facet buckets in the same response as paged hits, enabling the Blazor UI to render the full facet panel immediately for the current query.

### 2.3 Assumptions
- Providers may contribute different facet names; the system should support heterogeneous facet capabilities.
- Facet names and values are normalized (lowercase) consistently with existing indexing rules.
- Result set size may be large; facet responses must be bounded via configurable bucket limits.

### 2.4 Constraints
- Elasticsearch requires `nested` mapping for correct aggregation over `name`/`value` pairs.
- Returning unbounded facet name/value buckets can be expensive; limits (`size`) must be applied.
- Migration requires reindexing (breaking change to index mapping).

## 3. Component / service design (high level)

### 3.1 Components
- Ingestion / indexing pipeline
  - Transforms canonical facet dictionary into flat `nested` entries at index time.
- Elasticsearch index
  - Stores nested facets and supports nested aggregations.
- Query Service
  - Executes search queries and returns:
    - paged hits
    - facet aggregations (discovered facet names and value buckets)
- Blazor UI
  - Renders facet panel from aggregations rather than inferring facets from hits.

### 3.2 Data flows
1. Provider builds `CanonicalDocument` and populates `Facets`.
2. Indexer transforms `Facets` into `nested` facet entries on the indexed document.
3. Query Service issues `_search` with:
   - `query` (must/filter)
   - `from/size` or `search_after` for paging hits
   - `aggs` for nested facet discovery
4. UI displays hits for the selected page and facet buckets for the entire matched set.

### 3.3 Key decisions
- Use nested `{ name, value }` entries to enable facet-name discovery at query time.
- Treat facet computation as an aggregation concern independent of hit paging.
- Apply configurable caps for number of facet names and values returned per query.

## 4. Functional requirements
- FR1: For any search query, the response MUST include facet name buckets discovered from the matched result set.
- FR2: For each returned facet name, the response MUST include value buckets and document counts.
- FR3: Facet buckets MUST reflect the full matched result set for the query and filters, independent of hit paging.
- FR4: The system MUST support different providers contributing different facet names.

## 5. Non-functional requirements
- NFR1: Facet aggregation latency SHOULD be acceptable for interactive UI usage.
- NFR2: The system MUST bound facet bucket sizes (facet names and values) to prevent excessive response payloads.
- NFR3: The solution MUST avoid mapping explosion and support incremental introduction of new facet names.

## 6. Data model

### 6.1 Indexed facet representation
- Replace (or supplement) dynamic object mapping:
  - `facets.<dynamicName>: ["value1", "value2"]`
- With nested entries:
  - `facets: [{"name":"provider","value":"ukho"}, {"name":"statuscode","value":"a1"}]`

### 6.2 Elasticsearch mapping (conceptual)
- `facets` is mapped as `nested`.
- `facets.name` is `keyword`.
- `facets.value` is `keyword`.

## 7. Interfaces & integration

### 7.1 Query API response shape (conceptual)
- The Query Service response SHOULD expose facet results as:
  - `facets: { <facetName>: [ { value, count }, ... ] }`
  - plus paging metadata and the current page of hits.

### 7.2 Elasticsearch query shape (example)

```json
{
  "from": 0,
  "size": 25,
  "query": {
    "bool": {
      "must": [
        { "multi_match": { "query": "<q>", "fields": ["searchText", "content"] } }
      ],
      "filter": [
        { "term": { "documentType": "<type>" } }
      ]
    }
  },
  "aggs": {
    "facets": {
      "nested": { "path": "facets" },
      "aggs": {
        "names": {
          "terms": { "field": "facets.name", "size": 100 },
          "aggs": {
            "values": {
              "terms": { "field": "facets.value", "size": 50 }
            }
          }
        }
      }
    }
  }
}
```

## 8. Observability (logging/metrics/tracing)
- Log aggregation execution time and response sizes at the Query Service.
- Emit metrics for facet aggregation duration and bucket counts.

## 9. Security & compliance
- Ensure facet values returned are authorized for the caller if the underlying documents are subject to authorization.
- Avoid leaking sensitive metadata via facet buckets.

## 10. Testing strategy
- Unit tests for transformation from canonical facet dictionary to nested entries.
- Integration tests for Elasticsearch mapping + nested aggregation correctness.
- UI tests to verify facet panel renders from aggregations and is stable across paging.

## 11. Rollout / migration
- Introduce a new index version with the nested facet mapping.
- Reindex existing documents.
- Switch Query Service to use nested aggregations.
- Validate UI feature flags / backward compatibility if legacy indices remain.

## 12. Open questions
- What are the maximum allowed facet names and values per query for acceptable performance?
- Should facet names/values be further normalized (e.g., trimming, casing) beyond current behavior?
- How should facet ordering be determined (alphabetical, count-desc, curated)?
