using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StudioApiHost.Tests.TestDoubles;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using UKHO.Search.Studio.Ingestion;
using UKHO.Search.Studio.Providers;
using Xunit;

namespace StudioApiHost.Tests
{
    public sealed class StudioApiHostIngestionEndpointTests
    {
        [Fact]
        public async Task GetIngestionPayloadById_returns_wrapped_payload_for_known_provider_and_id()
        {
            var provider = new TestStudioIngestionProvider
            {
                FetchPayloadByIdHandler = id => Task.FromResult(
                    StudioIngestionFetchPayloadResult.Success(
                        new StudioIngestionPayloadEnvelope
                        {
                            Id = id,
                            Payload = JsonDocument.Parse("""
                                {
                                  "RequestType": "IndexItem"
                                }
                                """).RootElement.Clone()
                        }))
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetFromJsonAsync<StudioIngestionPayloadEnvelope>("/ingestion/test-provider/batch-123");

                response.ShouldNotBeNull();
                response.Id.ShouldBe("batch-123");
                response.Payload.GetProperty("RequestType").GetString().ShouldBe("IndexItem");
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetIngestionPayloadById_returns_success_when_a_long_running_operation_is_active()
        {
            var releaseOperation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new TestStudioIngestionProvider
            {
                FetchPayloadByIdHandler = id => Task.FromResult(
                    StudioIngestionFetchPayloadResult.Success(
                        new StudioIngestionPayloadEnvelope
                        {
                            Id = id,
                            Payload = JsonDocument.Parse("""
                                {
                                  "RequestType": "IndexItem"
                                }
                                """).RootElement.Clone()
                        })),
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 0 of 1.",
                        Completed = 0,
                        Total = 1
                    });

                    await releaseOperation.Task.WaitAsync(cancellationToken);

                    return StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                startResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

                _ = await WaitForOperationStateAsync(
                    app.GetTestClient(),
                    "/operations/active",
                    operation => operation.Status == StudioIngestionOperationStatuses.Running);

                var response = await app.GetTestClient().GetAsync("/ingestion/test-provider/batch-123");
                var body = await response.Content.ReadFromJsonAsync<StudioIngestionPayloadEnvelope>();

                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                body.ShouldNotBeNull();
                body.Id.ShouldBe("batch-123");

                releaseOperation.SetResult(true);
            }
            finally
            {
                releaseOperation.TrySetResult(true);
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetOperationById_returns_completed_operation_after_it_finishes()
        {
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = (progress, cancellationToken) =>
                {
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 2 of 2.",
                        Completed = 2,
                        Total = 2
                    });

                    return Task.FromResult(StudioIngestionOperationExecutionResult.Success("Processed 2 of 2.", 2, 2));
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                var acceptedOperation = await startResponse.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                acceptedOperation.ShouldNotBeNull();

                var completedOperation = await WaitForOperationStateAsync(
                    app.GetTestClient(),
                    $"/operations/{acceptedOperation.OperationId}",
                    operation => operation.Status == StudioIngestionOperationStatuses.Succeeded);

                completedOperation.OperationId.ShouldBe(acceptedOperation.OperationId);
                completedOperation.Completed.ShouldBe(2);
                completedOperation.Total.ShouldBe(2);
                completedOperation.CompletedUtc.ShouldNotBeNull();
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetIngestionContexts_returns_contexts_sorted_by_display_name()
        {
            var provider = new TestStudioIngestionProvider
            {
                GetContextsHandler = () => Task.FromResult(
                    new StudioIngestionContextsResponse
                    {
                        Provider = "test-provider",
                        Contexts =
                        [
                            new StudioIngestionContextResponse { Value = "2", DisplayName = "Zulu", IsDefault = false },
                            new StudioIngestionContextResponse { Value = "1", DisplayName = "Alpha", IsDefault = true }
                        ]
                    })
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetFromJsonAsync<StudioIngestionContextsResponse>("/ingestion/test-provider/contexts");

                response.ShouldNotBeNull();
                response.Provider.ShouldBe("test-provider");
                response.Contexts.Select(context => context.DisplayName).ToArray().ShouldBe(["Alpha", "Zulu"]);
                response.Contexts[0].Value.ShouldBe("1");
                response.Contexts[0].IsDefault.ShouldBeTrue();
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task PutIngestionContext_returns_bad_request_for_unknown_context()
        {
            var provider = new TestStudioIngestionProvider
            {
                GetContextsHandler = () => Task.FromResult(
                    new StudioIngestionContextsResponse
                    {
                        Provider = "test-provider",
                        Contexts =
                        [
                            new StudioIngestionContextResponse { Value = "12", DisplayName = "Admiralty", IsDefault = false }
                        ]
                    })
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().PutAsync("/ingestion/test-provider/context/999", content: null);
                var error = await response.Content.ReadFromJsonAsync<StudioIngestionErrorResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                error.ShouldNotBeNull();
                error.Message.ShouldContain("999");
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task PutIngestionContext_returns_accepted_operation_for_known_context()
        {
            var provider = new TestStudioIngestionProvider
            {
                GetContextsHandler = () => Task.FromResult(
                    new StudioIngestionContextsResponse
                    {
                        Provider = "test-provider",
                        Contexts =
                        [
                            new StudioIngestionContextResponse { Value = "12", DisplayName = "Admiralty", IsDefault = false }
                        ]
                    }),
                IndexContextHandler = (context, progress, cancellationToken) =>
                {
                    context.ShouldBe("12");
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 1 of 1.",
                        Completed = 1,
                        Total = 1
                    });

                    return Task.FromResult(StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1));
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().PutAsync("/ingestion/test-provider/context/12", content: null);
                var body = await response.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
                body.ShouldNotBeNull();
                body.OperationType.ShouldBe(StudioIngestionOperationTypes.ContextIndex);
                body.Context.ShouldBe("12");
                body.Status.ShouldBe(StudioIngestionOperationStatuses.Queued);
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task PostResetIndexingStatusForContext_returns_accepted_operation_for_known_context()
        {
            var provider = new TestStudioIngestionProvider
            {
                GetContextsHandler = () => Task.FromResult(
                    new StudioIngestionContextsResponse
                    {
                        Provider = "test-provider",
                        Contexts =
                        [
                            new StudioIngestionContextResponse { Value = "12", DisplayName = "Admiralty", IsDefault = false }
                        ]
                    }),
                ResetIndexingStatusForContextHandler = (context, progress, cancellationToken) =>
                {
                    context.ShouldBe("12");
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Reset indexing status for 2 items.",
                        Completed = 2,
                        Total = 2
                    });

                    return Task.FromResult(StudioIngestionOperationExecutionResult.Success("Reset indexing status for 2 items.", 2, 2));
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().PostAsync("/ingestion/test-provider/context/12/operations/reset-indexing-status", content: null);
                var body = await response.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
                body.ShouldNotBeNull();
                body.OperationType.ShouldBe(StudioIngestionOperationTypes.ResetIndexingStatus);
                body.Context.ShouldBe("12");
                body.Status.ShouldBe(StudioIngestionOperationStatuses.Queued);
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task PutIngestionAll_returns_accepted_operation_envelope_for_known_provider()
        {
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    await Task.Yield();
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 1 of 1.",
                        Completed = 1,
                        Total = 1
                    });

                    return StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                var body = await response.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
                body.ShouldNotBeNull();
                body.Provider.ShouldBe("test-provider");
                body.OperationType.ShouldBe(StudioIngestionOperationTypes.IndexAll);
                body.Status.ShouldBe(StudioIngestionOperationStatuses.Queued);
                Guid.TryParse(body.OperationId, out _).ShouldBeTrue();
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetOperationsActive_returns_running_operation_after_provider_wide_ingestion_starts()
        {
            var releaseOperation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 0 of 2.",
                        Completed = 0,
                        Total = 2
                    });

                    await releaseOperation.Task.WaitAsync(cancellationToken);

                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 2 of 2.",
                        Completed = 2,
                        Total = 2
                    });

                    return StudioIngestionOperationExecutionResult.Success("Processed 2 of 2.", 2, 2);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                startResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

                var activeOperation = await WaitForOperationStateAsync(
                    app.GetTestClient(),
                    "/operations/active",
                    operation => operation.Status == StudioIngestionOperationStatuses.Running);

                activeOperation.ShouldNotBeNull();
                activeOperation.Provider.ShouldBe("test-provider");
                activeOperation.OperationType.ShouldBe(StudioIngestionOperationTypes.IndexAll);
                activeOperation.Status.ShouldBe(StudioIngestionOperationStatuses.Running);
                activeOperation.Completed.ShouldBe(0);
                activeOperation.Total.ShouldBe(2);

                releaseOperation.SetResult(true);
            }
            finally
            {
                releaseOperation.TrySetResult(true);
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task SubmitIngestionPayload_returns_conflict_when_a_long_running_operation_is_active()
        {
            var releaseOperation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 0 of 1.",
                        Completed = 0,
                        Total = 1
                    });

                    await releaseOperation.Task.WaitAsync(cancellationToken);

                    return StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                startResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

                _ = await WaitForOperationStateAsync(
                    app.GetTestClient(),
                    "/operations/active",
                    operation => operation.Status == StudioIngestionOperationStatuses.Running);

                var response = await app.GetTestClient().PostAsJsonAsync(
                    "/ingestion/test-provider/payload",
                    new StudioIngestionPayloadEnvelope
                    {
                        Id = "batch-123",
                        Payload = JsonDocument.Parse("""
                            {
                              "RequestType": "IndexItem"
                            }
                            """).RootElement.Clone()
                    });

                var body = await response.Content.ReadFromJsonAsync<StudioIngestionOperationConflictResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
                body.ShouldNotBeNull();
                body.ActiveProvider.ShouldBe("test-provider");
                body.ActiveOperationType.ShouldBe(StudioIngestionOperationTypes.IndexAll);

                releaseOperation.SetResult(true);
            }
            finally
            {
                releaseOperation.TrySetResult(true);
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetOperationEvents_returns_live_updates_and_completes_after_terminal_event()
        {
            var allowCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    await allowCompletion.Task.WaitAsync(cancellationToken);
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 1 of 1.",
                        Completed = 1,
                        Total = 1
                    });

                    return StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                var acceptedOperation = await startResponse.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                acceptedOperation.ShouldNotBeNull();

                var responseTask = app.GetTestClient().GetAsync(
                    $"/operations/{acceptedOperation.OperationId}/events",
                    HttpCompletionOption.ResponseHeadersRead);

                var response = await responseTask;

                allowCompletion.SetResult(true);
                var body = await response.Content.ReadAsStringAsync();

                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                body.ShouldContain("\"status\":\"succeeded\"");
                body.ShouldContain("\"completed\":1");
            }
            finally
            {
                allowCompletion.TrySetResult(true);
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetIngestionPayloadById_returns_bad_request_for_unknown_provider()
        {
            var app = StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(CreateDefaultRulesConfiguration());
                });

            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetAsync("/ingestion/unknown-provider/batch-123");
                var error = await response.Content.ReadFromJsonAsync<StudioIngestionErrorResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                error.ShouldNotBeNull();
                error.Message.ShouldContain("unknown-provider");
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetIngestionPayloadById_returns_not_found_for_unknown_id()
        {
            var provider = new TestStudioIngestionProvider
            {
                FetchPayloadByIdHandler = id => Task.FromResult(StudioIngestionFetchPayloadResult.NotFound($"No payload was found for id '{id}'."))
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetAsync("/ingestion/test-provider/missing-id");
                var error = await response.Content.ReadFromJsonAsync<StudioIngestionErrorResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
                error.ShouldNotBeNull();
                error.Message.ShouldContain("missing-id");
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task SubmitIngestionPayload_returns_success_after_provider_accepts_payload()
        {
            var provider = new TestStudioIngestionProvider
            {
                SubmitPayloadHandler = request => Task.FromResult(StudioIngestionSubmitPayloadResult.Success("Payload submitted successfully."))
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().PostAsJsonAsync(
                    "/ingestion/test-provider/payload",
                    new StudioIngestionPayloadEnvelope
                    {
                        Id = "batch-123",
                        Payload = JsonDocument.Parse("""
                            {
                              "RequestType": "IndexItem"
                            }
                            """).RootElement.Clone()
                    });

                var body = await response.Content.ReadFromJsonAsync<StudioIngestionSubmitPayloadResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                body.ShouldNotBeNull();
                body.Accepted.ShouldBeTrue();
                provider.SubmittedRequests.Count.ShouldBe(1);
                provider.SubmittedRequests[0].Id.ShouldBe("batch-123");
                provider.SubmittedRequests[0].Payload.GetProperty("RequestType").GetString().ShouldBe("IndexItem");
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        private static WebApplication BuildApp(TestStudioIngestionProvider provider)
        {
            return StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(CreateDefaultRulesConfiguration());
                    builder.Services.AddProviderDescriptor<AdditionalProviderRegistrationMarker>(
                        new ProviderDescriptor("test-provider", "Test Provider", "Provider used by ingestion endpoint tests."));
                    builder.Services.AddSingleton<IStudioProvider>(provider);
                });
        }

        private static Dictionary<string, string?> CreateDefaultRulesConfiguration()
        {
            return new Dictionary<string, string?>
            {
                ["SkipAddsConfiguration"] = "true",
                ["rules:file-share:rule-1"] = """
                    {
                      "schemaVersion": "1.0",
                      "rule": {
                        "id": "rule-1",
                        "title": "Studio API host ingestion test rule",
                        "if": { "path": "id", "exists": true },
                        "then": { "keywords": { "add": [ "k" ] } }
                      }
                    }
                    """
            };
        }

        private sealed class TestStudioIngestionProvider : IStudioIngestionProvider
        {
            public Func<string, Task<StudioIngestionFetchPayloadResult>>? FetchPayloadByIdHandler { get; init; }

            public Func<Task<StudioIngestionContextsResponse>>? GetContextsHandler { get; init; }

            public Func<IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>>? IndexAllHandler { get; init; }

            public Func<string, IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>>? IndexContextHandler { get; init; }

            public string ProviderName => "test-provider";

            public Func<IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>>? ResetIndexingStatusHandler { get; init; }

            public Func<string, IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>>? ResetIndexingStatusForContextHandler { get; init; }

            public List<StudioIngestionPayloadEnvelope> SubmittedRequests { get; } = [];

            public Func<StudioIngestionPayloadEnvelope, Task<StudioIngestionSubmitPayloadResult>>? SubmitPayloadHandler { get; init; }

            public Task<StudioIngestionFetchPayloadResult> FetchPayloadByIdAsync(string id, CancellationToken cancellationToken = default)
            {
                return FetchPayloadByIdHandler is null
                    ? Task.FromResult(StudioIngestionFetchPayloadResult.NotFound($"No payload was found for id '{id}'."))
                    : FetchPayloadByIdHandler(id);
            }

            public Task<StudioIngestionContextsResponse> GetContextsAsync(CancellationToken cancellationToken = default)
            {
                return GetContextsHandler is null
                    ? Task.FromResult(new StudioIngestionContextsResponse { Provider = ProviderName, Contexts = [] })
                    : GetContextsHandler();
            }

            public Task<StudioIngestionOperationExecutionResult> IndexAllAsync(
                IProgress<StudioIngestionOperationProgressUpdate> progress,
                CancellationToken cancellationToken = default)
            {
                return IndexAllHandler is null
                    ? Task.FromResult(StudioIngestionOperationExecutionResult.Success("Processed 0 of 0.", 0, 0))
                    : IndexAllHandler(progress, cancellationToken);
            }

            public Task<StudioIngestionOperationExecutionResult> IndexContextAsync(
                string context,
                IProgress<StudioIngestionOperationProgressUpdate> progress,
                CancellationToken cancellationToken = default)
            {
                return IndexContextHandler is null
                    ? Task.FromResult(StudioIngestionOperationExecutionResult.Success("Processed 0 of 0.", 0, 0))
                    : IndexContextHandler(context, progress, cancellationToken);
            }

            public Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusAsync(
                IProgress<StudioIngestionOperationProgressUpdate> progress,
                CancellationToken cancellationToken = default)
            {
                return ResetIndexingStatusHandler is null
                    ? Task.FromResult(StudioIngestionOperationExecutionResult.Success("Reset indexing status for 0 items.", 0, 0))
                    : ResetIndexingStatusHandler(progress, cancellationToken);
            }

            public Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusForContextAsync(
                string context,
                IProgress<StudioIngestionOperationProgressUpdate> progress,
                CancellationToken cancellationToken = default)
            {
                return ResetIndexingStatusForContextHandler is null
                    ? Task.FromResult(StudioIngestionOperationExecutionResult.Success("Reset indexing status for 0 items.", 0, 0))
                    : ResetIndexingStatusForContextHandler(context, progress, cancellationToken);
            }

            public async Task<StudioIngestionSubmitPayloadResult> SubmitPayloadAsync(StudioIngestionPayloadEnvelope request, CancellationToken cancellationToken = default)
            {
                SubmittedRequests.Add(new StudioIngestionPayloadEnvelope
                {
                    Id = request.Id,
                    Payload = request.Payload.Clone()
                });

                return SubmitPayloadHandler is null
                    ? StudioIngestionSubmitPayloadResult.Success("Payload submitted successfully.")
                    : await SubmitPayloadHandler(request).ConfigureAwait(false);
            }
        }

        private static async Task<StudioIngestionOperationStateResponse> WaitForOperationStateAsync(
            HttpClient client,
            string path,
            Func<StudioIngestionOperationStateResponse, bool> predicate)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var response = await client.GetAsync(path);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var operation = await response.Content.ReadFromJsonAsync<StudioIngestionOperationStateResponse>();
                    if (operation is not null && predicate(operation))
                    {
                        return operation;
                    }
                }

                await Task.Delay(50);
            }

            throw new ShouldAssertException($"Timed out waiting for operation state at '{path}'.");
        }
    }
}
