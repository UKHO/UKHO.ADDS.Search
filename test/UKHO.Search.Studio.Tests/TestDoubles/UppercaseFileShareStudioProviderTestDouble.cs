using UKHO.Search.Studio;

namespace UKHO.Search.Studio.Tests.TestDoubles
{
    internal sealed class UppercaseFileShareStudioProviderTestDouble : IStudioProvider
    {
        public string ProviderName => "FILE-SHARE";
    }
}
