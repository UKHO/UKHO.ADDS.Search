using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using FileShareEmulator.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FileShareEmulator.Tests
{
    public sealed class BatchFilesApiTests
    {
        [Fact]
        public async Task GetBatchFiles_WhenBlobDoesNotExist_ReturnsNotFoundWithoutListingContainer()
        {
            var batchId = Guid.NewGuid().ToString("D").ToUpperInvariant();
            var transport = new RecordingTransport(404);
            var blobServiceClient = CreateBlobServiceClient(transport);

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["environment"] = "test-container"
            });
            builder.Services.AddSingleton(blobServiceClient);

            var app = builder.Build();
            app.MapBatchFilesApi();

            try
            {
                await app.StartAsync();
                using var client = app.GetTestClient();

                var response = await client.GetAsync($"/batch/{batchId}/files");

                response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
                transport.Requests.Count.ShouldBe(2);
                transport.Requests.ShouldAllBe(request => !request.Uri.Query.Contains("comp=list", StringComparison.OrdinalIgnoreCase));
                transport.Requests[0].Uri.AbsolutePath.ShouldContain(batchId.ToLowerInvariant());
                transport.Requests[1].Uri.AbsolutePath.ShouldContain(batchId);
            }
            finally
            {
                await app.DisposeAsync();
            }
        }

        private static BlobServiceClient CreateBlobServiceClient(HttpPipelineTransport transport)
        {
            var options = new BlobClientOptions
            {
                Transport = transport,
                Retry =
                {
                    MaxRetries = 0
                }
            };

            var credential = new StorageSharedKeyCredential("testaccount", Convert.ToBase64String(new byte[32]));
            return new BlobServiceClient(new Uri("https://example.blob.core.windows.net"), credential, options);
        }

        private sealed class RecordingTransport : HttpPipelineTransport
        {
            private readonly int _statusCode;

            public RecordingTransport(int statusCode)
            {
                _statusCode = statusCode;
            }

            public List<RecordedRequest> Requests { get; } = [];

            public override Request CreateRequest()
            {
                return new RecordingPipelineRequest();
            }

            public override void Process(HttpMessage message)
            {
                ProcessAsync(message)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
            }

            public override async ValueTask ProcessAsync(HttpMessage message)
            {
                var request = message.Request;

                byte[]? body = null;
                if (request.Content is not null)
                {
                    using var ms = new MemoryStream();

                    await request.Content.WriteToAsync(ms, CancellationToken.None)
                                 .ConfigureAwait(false);
                    body = ms.ToArray();
                }

                Requests.Add(new RecordedRequest(request.Method.Method, request.Uri.ToUri(), body));
                message.Response = new SimpleResponse(_statusCode);
            }
        }

        private sealed class RecordingPipelineRequest : Request
        {
            private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);
            private RequestUriBuilder _uri = new();

            public override RequestContent? Content { get; set; }

            public override string ClientRequestId { get; set; } = string.Empty;

            public override RequestMethod Method { get; set; }

            public override RequestUriBuilder Uri
            {
                get => _uri;
                set => _uri = value;
            }

            public override void Dispose()
            {
            }

            protected override void SetHeader(string name, string value)
            {
                _headers[name] = [value];
            }

            protected override bool TryGetHeader(string name, out string? value)
            {
                if (_headers.TryGetValue(name, out var values) && values.Count > 0)
                {
                    value = values[0];
                    return true;
                }

                value = null;
                return false;
            }

            protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values)
            {
                if (_headers.TryGetValue(name, out var list))
                {
                    values = list;
                    return true;
                }

                values = null;
                return false;
            }

            protected override bool ContainsHeader(string name)
            {
                return _headers.ContainsKey(name);
            }

            protected override IEnumerable<HttpHeader> EnumerateHeaders()
            {
                foreach (var (name, values) in _headers)
                {
                    foreach (var value in values)
                    {
                        yield return new HttpHeader(name, value);
                    }
                }
            }

            protected override void AddHeader(string name, string value)
            {
                if (!_headers.TryGetValue(name, out var values))
                {
                    values = [];
                    _headers[name] = values;
                }

                values.Add(value);
            }

            protected override bool RemoveHeader(string name)
            {
                return _headers.Remove(name);
            }
        }

        private sealed class RecordedRequest
        {
            public RecordedRequest(string method, Uri uri, byte[]? body)
            {
                Method = method;
                Uri = uri;
                Body = body;
            }

            public string Method { get; }

            public Uri Uri { get; }

            public byte[]? Body { get; }
        }

        private sealed class SimpleResponse : Response
        {
            private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);
            private readonly int _status;

            public SimpleResponse(int status)
            {
                _status = status;
            }

            public override int Status => _status;

            public override string ReasonPhrase => string.Empty;

            public override Stream? ContentStream { get; set; }

            public override string ClientRequestId { get; set; } = string.Empty;

            public override void Dispose()
            {
            }

            protected override bool ContainsHeader(string name)
            {
                return _headers.ContainsKey(name);
            }

            protected override IEnumerable<HttpHeader> EnumerateHeaders()
            {
                foreach (var (name, values) in _headers)
                {
                    foreach (var value in values)
                    {
                        yield return new HttpHeader(name, value);
                    }
                }
            }

            protected override bool TryGetHeader(string name, out string? value)
            {
                if (_headers.TryGetValue(name, out var values) && values.Count > 0)
                {
                    value = values[0];
                    return true;
                }

                value = null;
                return false;
            }

            protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values)
            {
                if (_headers.TryGetValue(name, out var list))
                {
                    values = list;
                    return true;
                }

                values = null;
                return false;
            }
        }
    }
}
