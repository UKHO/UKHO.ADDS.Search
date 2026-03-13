using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
	public class RulesSnapshotStoreTests
	{
		[Fact]
		public void LoadFileShareRules_WhenFileMissing_ReturnsError()
		{
			var env = new TestWebHostEnvironment
			{
				ContentRootFileProvider = new NullFileProvider(),
			};

          var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());

			var snapshot = store.LoadFileShareRules();

			snapshot.IsLoaded.ShouldBeFalse();
			snapshot.Error.ShouldNotBeNull();
          snapshot.Error!.Message.ToLowerInvariant().ShouldContain("not found");
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
	}
}
