using UKHO.Search.Studio.Providers;

namespace UKHO.Search.Studio.Tests.TestDoubles
{
    internal sealed class FileShareStudioProviderTestDouble : IStudioProvider
    {
        public string ProviderName => "file-share";
    }
}
