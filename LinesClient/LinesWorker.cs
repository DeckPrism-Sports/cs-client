#region Usings
using DPSports.Configuration;
using DPSports.Entity.DTO;
using DPSports.Feed;
using DPSports.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace LinesClient
{
    public class LinesWorker : IWorker
    {
        private readonly IRabbitService _rabbitService;
        private readonly IApiClient _apiClient;
        private static readonly object _newLinesLock = new object();

        public LinesWorker(IRabbitService rabbitService, IApiClient apiClient)
        {
            _apiClient = apiClient;          
            _rabbitService = rabbitService;           
        }

        #region Public Methods
        public async Task<int> MainLoop(CancellationToken token = default)
        {
            try
            {
                //this runs forever
                while(!token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                    await MainFunc();
                }
            }
            catch(Exception ex)
            {
                LogManager.Error(ex, $"Error in {nameof(MainLoop)}", null);
            }
            finally
            {
                ExitHandler();
            }
            return 1;
        }
        #endregion

        #region Private Methods
        private void ExitHandler()
        {
            //check point before exit
        }

        private async Task MainFunc()
        {
            var cst = new CancellationTokenSource();

            Task.WaitAny(ProcessFeed(cst.Token));

            cst.Cancel();
            cst.Dispose();

            Console.WriteLine("All Games Suspended");
            await Task.Run(delegate
            {
                SuspendAllGames();
            });
        }
        private async Task<bool> ProcessFeed(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                while(!token.IsCancellationRequested)
                {
                    //this gets called once upon startup
                    ProcessHttpMessages(token);
                    //this susbcribes to the rabbit exchange and gets the updates
                    await ProcessRabbitMessages(token);
                }
                return false;
            }
            catch(Exception ex)
            {
                LogManager.Error(ex, $"{nameof(ProcessFeed)} Failed", null);
                throw ex;
            }
        }
        private void ProcessHttpMessages(CancellationToken token)
        {
            var changedGames = _apiClient.GetGames(DateTime.Today, DateTime.Today.AddDays(2)).Result;
            var saveResults = new List<Game>(0);

            foreach(var changed in changedGames)
            {
                //You can compare the local store against the api results to determine if a game has changes                
                var hasChanges = true;

                if(!hasChanges)
                {
                    LogReceivedGame(changed, "api", "No changes found");
                }
                else
                {
                    LogReceivedGame(changed, "api", "Local storage updates needed");
                    saveResults.Add(changed);
                }
            }

            if(saveResults.Count > 0)
            {
                //do something with the games that have changes
            }
        }

        private async Task<bool> ProcessRabbitMessages(CancellationToken token)
        {            
            await _rabbitService.ReceiveMessages(ConfigurationManager.Rabbit.RabbitRoutingKey, (DataResult<Game> result, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();

                if(!result.Success)
                {
                    LogManager.Error($"Rabbit receive message failed. DPSports reported an error: {result.Exception}", null);
                    return;
                }

                var changed = result.Content;
                //You can compare the local store against the api results to determine if a game has changes                
                var hasChanges = true;

                if(!hasChanges)
                {
                    LogReceivedGame(changed, "rabbit", "No changes found");
                }
                else
                {
                    LogReceivedGame(changed, "rabbit", "Local storage updates needed");
                }

                ProcessChanges(token);

            }, token);

            //we are done now
            return true;
        }
        private static void LogReceivedGame(Game changed, string source, string message)
        {
            var eventId = 0L;
            var structuredLog = "";
            var propVals = new List<object>(5);            

            if(changed != null)
            {
                var incommingMarkets = LogUtils.ComposeMarketsForLogging(changed.Markets);
                eventId = changed.Id;               

                if(incommingMarkets?.Count > 0)
                {
                    structuredLog += "IncomingMarkets {@IncomingMarkets} ";
                    propVals.Add(incommingMarkets);
                }
            }

            LogManager.Info($"Received game {changed.AwayRotation} {changed.AwayName} at {changed.HomeRotation} {changed.HomeName} from {source}. {message}. " + structuredLog.Trim(), eventId, propVals.ToArray());
        }


        private void ProcessChanges(CancellationToken token)
        {
           // Do some processing of changes here         
        }           

        private void SuspendAllGames()
        {
            //When we stop the app we might want to suspend all lines.
        }
        #endregion
    }
}