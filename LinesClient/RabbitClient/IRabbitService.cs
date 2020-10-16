using System;
using System.Threading;
using System.Threading.Tasks;


namespace DPSports.Feed
{
    public interface IRabbitService
    {
        Task ReceiveMessages<T>(string routingKey, Action<T, CancellationToken> receivedMsgAction, CancellationToken token) where T : class;       
    }
}