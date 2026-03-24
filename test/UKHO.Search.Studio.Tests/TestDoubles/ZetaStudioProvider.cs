using UKHO.Search.Studio.Providers;

namespace UKHO.Search.Studio.Tests.TestDoubles
{
    internal sealed class ZetaStudioProvider : IStudioProvider
    {
        public string ProviderName => "z-provider";
    }
}
