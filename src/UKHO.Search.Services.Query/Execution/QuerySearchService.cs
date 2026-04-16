using Microsoft.Extensions.Logging;
using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Results;
using UKHO.Search.Services.Query.Abstractions;

namespace UKHO.Search.Services.Query.Execution
{
    /// <summary>
    /// Coordinates query planning and execution into one host-friendly application service.
    /// </summary>
    public sealed class QuerySearchService : IQuerySearchService
    {
        private readonly IQueryPlanService _queryPlanService;
        private readonly IQueryPlanExecutor _queryPlanExecutor;
        private readonly ILogger<QuerySearchService> _logger;

        /// <summary>
        /// Initializes the query search service with the planner and execution adapter required for end-to-end query handling.
        /// </summary>
        /// <param name="queryPlanService">The planner that converts raw query text into a repository-owned query plan.</param>
        /// <param name="queryPlanExecutor">The execution adapter that translates the query plan into search-engine behavior.</param>
        /// <param name="logger">The logger used to emit structured search diagnostics and failures.</param>
        public QuerySearchService(IQueryPlanService queryPlanService, IQueryPlanExecutor queryPlanExecutor, ILogger<QuerySearchService> logger)
        {
            // Capture the injected collaborators so each search request runs through the same planner and execution path.
            _queryPlanService = queryPlanService ?? throw new ArgumentNullException(nameof(queryPlanService));
            _queryPlanExecutor = queryPlanExecutor ?? throw new ArgumentNullException(nameof(queryPlanExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Plans and executes a query from the supplied raw query text.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the caller.</param>
        /// <param name="cancellationToken">The cancellation token that stops planning or execution when the caller no longer needs the result.</param>
        /// <returns>The executed query result.</returns>
        public async Task<QuerySearchResult> SearchAsync(string? queryText, CancellationToken cancellationToken)
        {
            try
            {
                // Produce the repository-owned query plan first so the execution adapter never sees host-specific request types.
                var plan = await _queryPlanService.PlanAsync(queryText, cancellationToken)
                    .ConfigureAwait(false);

                // Execute the plan through the injected infrastructure adapter to keep the application-service boundary clean.
                var result = await _queryPlanExecutor.SearchAsync(plan, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "Executed query search. Total={Total} DurationMs={DurationMs}",
                    result.Total,
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                // Log the failure at the application-service boundary so host callers can correlate planner and executor failures.
                _logger.LogError(ex, "Failed to execute a query search for the supplied query text.");
                throw;
            }
        }
    }
}
