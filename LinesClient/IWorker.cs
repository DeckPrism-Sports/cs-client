using System.Threading;
using System.Threading.Tasks;

namespace LinesClient
{
    public interface IWorker
    {
        Task<int> MainLoop(CancellationToken token = default);
    }
}
