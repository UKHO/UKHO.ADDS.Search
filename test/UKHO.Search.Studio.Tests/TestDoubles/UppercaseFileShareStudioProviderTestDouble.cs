using UKHO.Search.Studio.Providers;

namespace UKHO.Search.Studio.Tests.TestDoubles
{
    internal sealed class UppercaseFileShareStudioProviderTestDouble : IStudioProvider
    {
        public string ProviderName => "FILE-SHARE";
    }
}
