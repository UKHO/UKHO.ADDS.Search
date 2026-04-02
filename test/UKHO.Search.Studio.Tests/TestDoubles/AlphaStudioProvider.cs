using UKHO.Search.Studio.Providers;

namespace UKHO.Search.Studio.Tests.TestDoubles
{
    internal sealed class AlphaStudioProvider : IStudioProvider
    {
        public string ProviderName => "a-provider";
    }
}
