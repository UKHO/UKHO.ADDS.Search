using System.Threading;
using System.Threading.Tasks;

namespace UKHO.Search.Studio.Providers.FileShare
{
    public interface IFileShareStudioQueueWriter
    {
        Task SubmitAsync(string payloadJson, CancellationToken cancellationToken = default);
    }
}
