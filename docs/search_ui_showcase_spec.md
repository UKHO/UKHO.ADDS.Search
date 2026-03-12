# Developer Search UI Showcase -- Requirements Specification

## 1. Purpose

This document specifies the requirements for a **developer‑oriented
search showcase UI** designed to demonstrate the capabilities of the new
search platform and replace legacy search tools.

The application is intentionally **minimal and focused**, not intended
to become a long‑lived operational product. The goal is to clearly
demonstrate:

-   Fast and relevant search
-   Faceted refinement
-   Rich inspection of individual results
-   Support for geographic data visualization

The design draws inspiration from **Kibana Discover**, but intentionally
avoids its complexity.

------------------------------------------------------------------------

# 2. Core Design Principles

The interface should prioritise:

-   **Immediate query interaction**
-   **Fast refinement using facets**
-   **Clear result inspection**
-   **Minimal navigation complexity**
-   **High information density suitable for developers**

The UI workflow is centred around the loop:

    Query → View Results → Refine with Facets → Inspect Result → Repeat

There will be **no traditional left‑hand application navigation**.

The application will consist of **a single primary page** dedicated to
search exploration.

------------------------------------------------------------------------

# 3. High Level Layout

The application layout consists of four main areas:

1.  Search Bar
2.  Facet Panel
3.  Results Panel
4.  Details Panel

## Layout Diagram

    ┌─────────────────────────────────────────────────────────────┐
    │ Search Bar + Active Filters + Query Metadata                │
    └─────────────────────────────────────────────────────────────┘
    ┌───────────────┬───────────────────────────────┬──────────────┐
    │               │                               │              │
    │               │                               │              │
    │   Facets      │        Results List           │   Details    │
    │   Panel       │                               │   Panel      │
    │               │                               │              │
    │               │                               │              │
    └───────────────┴───────────────────────────────┴──────────────┘
    ┌─────────────────────────────────────────────────────────────┐
    │ Status Bar                                                  │
    └─────────────────────────────────────────────────────────────┘

Panel purposes:

  Panel           Purpose
  --------------- ---------------------------------------
  Search Bar      Enter and manage queries
  Facets Panel    Narrow and refine results
  Results Panel   Display search hits
  Details Panel   Show full details for selected result
  Status Bar      Number of results returned, time taken to execute

------------------------------------------------------------------------

# 4. Search Bar

The search bar sits at the top of the interface and is the **primary
interaction entry point**.

## Features

-   Free‑text search input
-   Immediate query execution
-   Display of active filters
-   Result count
-   Query execution time

## Example Layout

    ┌─────────────────────────────────────────────────────────────┐
    │ 🔎 Search: [ wreck north sea pipeline ]                     │
    │                                                             │
    │ Filters: Region: North Sea ×  Type: Wreck ×                 │
    │                                                             │
    │ 143 results | 37 ms                                         │
    └─────────────────────────────────────────────────────────────┘

## Requirements

-   Search queries should update the URL to allow bookmarking and
    sharing
-   Active filters should appear as **removable filter chips**
-   Query execution time and hit count must be displayed

------------------------------------------------------------------------

# 5. Facets Panel

Facets enable users to **narrow results quickly without modifying the
search query**.

Facets appear in a **left column adjacent to the results**.

## Example Layout

    Facets
    ----------------------
    Region
      North Sea (54)
      Baltic (12)
      Atlantic (8)

    Type
      Wreck (39)
      Pipeline (22)
      Cable (17)

    Status
      Active (61)
      Historic (13)

## Requirements

-   Facets must show **counts**
-   Facets must support **multi‑selection**
-   Selected facets should update the search results immediately
-   Selected facet values should also appear as **chips in the search
    bar area**
-   Facet groups must be **collapsible**

------------------------------------------------------------------------

# 6. Results Panel

The central column shows the **list of matching results**.

Results should provide enough context to allow users to evaluate
relevance without opening each result.

## Example Result Layout

    Result
    --------------------------------------------------
    Name: SS Example Wreck
    Type: Shipwreck
    Region: North Sea
    Coordinates: 54.231N 1.221E
    Source: Hydrographic Office
    Matched fields: name, description
    --------------------------------------------------

## Requirements

Each result row should include:

-   Title / name
-   Feature type
-   Geographic region
-   Summary of data (to be determined in a later spec)
-   Highlighted matched fields

## Behaviour

-   Clicking a result selects it
-   The selected result is highlighted
-   Selecting a result populates the **Details Panel**

------------------------------------------------------------------------

# 7. Details Panel

The details panel shows **rich information about the selected result**.

This panel remains visible while users browse the results list.

## Layout

    Details
    ----------------------------------
    Title
    Type
    Region
    Coordinates

    Map (OpenStreetMap)

    Attributes
    Raw Fields (JSON view optional)

## Map Integration

The details panel includes a **contextual map** showing the selected
result.

### Map Technology

-   **OpenStreetMap**
-   Integrated using Radzen-compatible Leaflet implementation
-   Based on approach described here:

https://forum.radzen.com/t/open-street-map/19131

### Map Requirements

-   Display result location or geometry
-   Center map on the selected result
-   Show marker or geometry overlay
-   Basic zoom and pan interaction

The map is **contextual only** and not used as a primary search
interaction tool.

------------------------------------------------------------------------

# 8. Interaction Flow

The user workflow is expected to follow:

    1. Enter search query
    2. Review initial results
    3. Narrow results using facets
    4. Select result
    5. Inspect details and map
    6. Adjust filters or query
    7. Repeat

------------------------------------------------------------------------

# 9. Performance Indicators

The UI should display query performance metadata for developer
transparency.

## Required Metrics

-   Result count
-   Query execution time

Example:

    143 results | 37 ms

------------------------------------------------------------------------

# 10. Empty State

Before a query is entered, the UI should present guidance.

Example:

    Start typing to search the dataset

    Examples:
    wreck north sea
    pipeline norway
    cable baltic

------------------------------------------------------------------------

# 11. Non‑Goals

The showcase application intentionally excludes:

-   Full data management UI
-   Index administration
-   Configuration pages
-   Complex query builders
-   Persistent user preferences

The goal is a **focused demonstration tool**.

------------------------------------------------------------------------

# 12. Technology Stack

  Component         Technology
  ----------------- ---------------------------------------
  UI Framework      Blazor
  UI Controls       Radzen
  Map               OpenStreetMap
  Map Integration   Leaflet via Radzen-compatible wrapper
  Backend           Existing search API

------------------------------------------------------------------------

# 13. Future Enhancements (Optional)

Possible extensions if needed:

-   Spatial filtering
-   Result clustering on map
-   Query inspector panel
-   Export results
-   Saved queries

These are **not required for the initial showcase**.

------------------------------------------------------------------------

# 14. Summary

The application is a **single‑page search exploration tool** designed to
demonstrate the capabilities of a modern search system.

Key characteristics:

-   Minimal UI
-   Fast interaction
-   Faceted refinement
-   Rich result inspection
-   Contextual geographic visualisation

The design deliberately prioritises **clarity and demonstration value**
over configurability or long‑term extensibility.
