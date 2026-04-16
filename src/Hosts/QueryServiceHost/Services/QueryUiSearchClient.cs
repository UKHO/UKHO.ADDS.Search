using QueryServiceHost.Models;
using UKHO.Search.Services.Query.Abstractions;

namespace QueryServiceHost.Services
{
    /// <summary>
    /// Adapts the host-local query UI contract onto the repository-owned query search application service.
    /// </summary>
    public sealed class QueryUiSearchClient : IQueryUiSearchClient
    {
        private readonly IQuerySearchService _querySearchService;
        private readonly ILogger<QueryUiSearchClient> _logger;

        /// <summary>
        /// Initializes the host search client with the repository-owned query search application service.
        /// </summary>
        /// <param name="querySearchService">The application service that plans and executes repository-owned query searches.</param>
        /// <param name="logger">The logger used to emit structured host-adapter diagnostics.</param>
        public QueryUiSearchClient(IQuerySearchService querySearchService, ILogger<QueryUiSearchClient> logger)
        {
            // Capture the injected collaborators once so the host adapter remains a thin composition-layer bridge.
            _querySearchService = querySearchService ?? throw new ArgumentNullException(nameof(querySearchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a query UI search request through the repository-owned query pipeline.
        /// </summary>
        /// <param name="request">The host-local query UI request that contains the user query text and current facet state.</param>
        /// <param name="cancellationToken">The cancellation token that stops the search when the caller no longer needs the result.</param>
        /// <returns>The host-local query response projected from the repository-owned query result.</returns>
        public async Task<QueryResponse> SearchAsync(QueryRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.SelectedFacets.Values.Any(static values => values.Count > 0))
            {
                // Surface the current limitation explicitly so contributors can see that slice one ignores host facet selections.
                _logger.LogInformation("Query UI facet selections were supplied but are not yet translated by the slice-one query pipeline.");
            }

            // Delegate the real query work to the application service so the host remains free of planning and Elasticsearch logic.
            var result = await _querySearchService.SearchAsync(request.QueryText, cancellationToken)
                .ConfigureAwait(false);

            return new QueryResponse
            {
                Hits = result.Hits.Select(static hit => new Hit
                {
                    Title = hit.Title,
                    Type = hit.Type,
                    Region = hit.Region,
                    MatchedFields = hit.MatchedFields.ToArray(),
                    Raw = hit.Raw
                }).ToArray(),
                Facets = Array.Empty<FacetGroup>(),
                Total = result.Total,
                Duration = result.Duration
            };
        }
    }
}
