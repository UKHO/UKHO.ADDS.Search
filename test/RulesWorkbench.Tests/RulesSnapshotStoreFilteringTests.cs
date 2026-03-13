using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
	public class RulesSnapshotStoreFilteringTests
	{
		[Fact]
		public void GetFileShareRuleSummaries_WhenQueryMatchesId_ReturnsSingleMatch()
		{
			var env = new TestWebHostEnvironment
			{
				ContentRootFileProvider = new InMemoryFileProvider(new Dictionary<string, string>
				{
					["ingestion-rules.json"] = "{\"rules\":{\"file-share\":[{\"id\":\"a\",\"description\":\"first\"},{\"id\":\"b\",\"description\":\"second\"}]}}",
				}),
			};

          var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());

			var result = store.GetFileShareRuleSummaries("b");

			result.Count.ShouldBe(1);
			result[0].Id.ShouldBe("b");
			result[0].Index.ShouldBe(1);
		}

		[Fact]
		public void UpdateFileShareRuleJson_WhenJsonValid_UpdatesInMemoryRule()
		{
			var env = new TestWebHostEnvironment
			{
				ContentRootFileProvider = new InMemoryFileProvider(new Dictionary<string, string>
				{
					["ingestion-rules.json"] = "{\"rules\":{\"file-share\":[{\"id\":\"a\",\"description\":\"first\"}]}}",
				}),
			};

			var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());
			store.LoadFileShareRules();

			var updateResult = store.UpdateFileShareRuleJson(0, "{\"id\":\"a\",\"description\":\"updated\"}");

			updateResult.IsValid.ShouldBeTrue();
			var rules = store.GetFileShareRuleSummaries(null);
			rules.Count.ShouldBe(1);
			rules[0].Description.ShouldBe("updated");
		}

		[Fact]
		public void UpdateFileShareRuleJson_WhenJsonInvalid_ReturnsInvalid()
		{
			var env = new TestWebHostEnvironment
			{
				ContentRootFileProvider = new InMemoryFileProvider(new Dictionary<string, string>
				{
					["ingestion-rules.json"] = "{\"rules\":{\"file-share\":[{\"id\":\"a\"}]}}",
				}),
			};

			var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());
			store.LoadFileShareRules();

			var updateResult = store.UpdateFileShareRuleJson(0, "{");

			updateResult.IsValid.ShouldBeFalse();
			updateResult.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
		}

		[Fact]
		public void GetFileShareRuleSummaries_WhenQueryMatchesDescription_PreservesOrder()
		{
			var env = new TestWebHostEnvironment
			{
				ContentRootFileProvider = new InMemoryFileProvider(new Dictionary<string, string>
				{
					["ingestion-rules.json"] = "{\"rules\":{\"file-share\":[{\"id\":\"a\",\"description\":\"match\"},{\"id\":\"b\",\"description\":\"match\"}]}}",
				}),
			};

          var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());

			var result = store.GetFileShareRuleSummaries("match");

			result.Count.ShouldBe(2);
			result[0].Id.ShouldBe("a");
			result[1].Id.ShouldBe("b");
			result[0].Index.ShouldBe(0);
			result[1].Index.ShouldBe(1);
		}

		private sealed class TestWebHostEnvironment : IWebHostEnvironment
		{
			public string ApplicationName { get; set; } = "RulesWorkbench.Tests";

			public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

			public string WebRootPath { get; set; } = string.Empty;

			public string EnvironmentName { get; set; } = "Development";

			public string ContentRootPath { get; set; } = string.Empty;

			public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
		}

		private sealed class InMemoryFileProvider : IFileProvider
		{
			private readonly IReadOnlyDictionary<string, byte[]> _files;

			public InMemoryFileProvider(IReadOnlyDictionary<string, string> files)
			{
				_files = files.ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetBytes(kv.Value), StringComparer.Ordinal);
			}

			public IDirectoryContents GetDirectoryContents(string subpath)
			{
				return NotFoundDirectoryContents.Singleton;
			}

			public IFileInfo GetFileInfo(string subpath)
			{
				var cleaned = subpath.TrimStart('/').Replace('\\', '/');
				if (_files.TryGetValue(cleaned, out var bytes))
				{
					return new InMemoryFileInfo(cleaned, bytes);
				}

				return new NotFoundFileInfo(subpath);
			}

			public IChangeToken Watch(string filter)
			{
				return NullChangeToken.Singleton;
			}

			private sealed class InMemoryFileInfo : IFileInfo
			{
				private readonly byte[] _bytes;

				public InMemoryFileInfo(string name, byte[] bytes)
				{
					Name = name;
					_bytes = bytes;
				}

				public bool Exists => true;
				public long Length => _bytes.LongLength;
				public string PhysicalPath => string.Empty;
				public string Name { get; }
				public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
				public bool IsDirectory => false;
				public Stream CreateReadStream() => new MemoryStream(_bytes, writable: false);
			}
		}
	}
}
