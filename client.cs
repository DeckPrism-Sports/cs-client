using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Text;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace DPTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () => {
                try
                {
                    var deck = new DeckUpdater();

                    Console.WriteLine(" [*] Pulling current game data from API...");
                    DateTime today = DateTime.Today;
                    var games = await deck.pullAPI(DateTime.Parse(today.ToString()));
                    games.ForEach(x => Console.Write(JsonConvert.SerializeObject(x, Formatting.Indented)));

                    Console.WriteLine("\n [*] Connecting to broker for live data...\n");
                    await deck.getLiveUpdate(x => {
                        Console.Write(JsonConvert.SerializeObject(x, Formatting.Indented));
                        Console.Write("\n\n [*] Waiting for a line change...\n");
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    System.Diagnostics.Debugger.Break();
                }

            }).Wait();
        }
    }
    public class DeckUpdater
    {
        private IConnection conn;
        private readonly string exchange;
        private readonly string UserName;
        private readonly string UserPass;
        private readonly string ApiKey;
        private readonly string VHost;
        private readonly string Sport;
        private readonly string Host;

        public DeckUpdater(string userName = "username", string userPass = "pass", string apiKey = "apikey", string vHost = "nfl_main", string sport = "nfl")
        {
            Host = "man1-phx2.deckprismsports.com";
            exchange = "nfl_upstream";
            UserName = userName;
            UserPass = userPass;
            ApiKey = apiKey;
            VHost = vHost;
            Sport = sport;
        }

        public async Task<List<DeckObject>> pullAPI(DateTime day)
        {
            using (var http = new HttpClient())
            {                
                var apiURI = $"https://{Host}/api/lines/{Sport}/{day.ToString("yyyyMMdd")}/?apikey={ApiKey}";
                var rawJson = await http.GetStringAsync(apiURI);
                return JsonConvert.DeserializeObject<List<DeckObject>>(rawJson);
            }
        }
        public async Task getLiveUpdate(Action<DeckObject> receivedMsgAction, CancellationToken cancellationToken = default)
        {
            var promise = new TaskCompletionSource<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var channel = connect())
                    {

                        var queueName = channel.QueueDeclare().QueueName;
                        channel.QueueBind(queue: queueName, exchange: exchange, routingKey: "");

                        Console.WriteLine("\n [*] Waiting for a line change...");

                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) => {
                            receivedMsgAction.Invoke(JsonConvert.DeserializeObject<DeckObject>(Encoding.UTF8.GetString(ea.Body.ToArray())));
                        };
                        consumer.Shutdown += (model, ea) => {
                            promise.SetResult(ea.ReplyText);
                        };


                        channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                        Console.WriteLine(await promise.Task);

                        if (channel.IsOpen)
                            channel.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex?.Message);
                }
            }

        }

        private IModel connect()
        {
            try
            {

                if (conn == null || !conn.IsOpen)
                {
                    var factory = new ConnectionFactory { Uri = new Uri($"amqps://{UserName}:{UserPass}@{Host}/{VHost}?heartbeat=15") };
                    conn = factory.CreateConnection();                    
                    conn.CallbackException += CallbackError;
                }

                var channel = conn.CreateModel();
                channel.ModelShutdown += ModelClosed;

                return channel;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }


            void CallbackError(object sender, CallbackExceptionEventArgs e)
            {
                Console.WriteLine(e.Detail.Select(x => $"{x.Key}: {x.Value}"));
            }

            void RecoveryError(object sender, ConnectionRecoveryErrorEventArgs e)
            {
                Console.WriteLine($"{nameof(RecoveryError)}");
            }

            void ModelClosed(object sender, ShutdownEventArgs e)
            {
                Console.WriteLine(JsonConvert.SerializeObject(e));
            }
        }

        public partial class DeckObject
        {
        [JsonProperty("deckID")]
        public long DeckId { get; set; }

        [JsonProperty("sport")]
        public string Sport { get; set; }

        [JsonProperty("group")]
        public string[] Group { get; set; }

        [JsonProperty("timeUTC")]
        public DateTimeOffset TimeUtc { get; set; }

        [JsonProperty("topRot")]
        public long TopRot { get; set; }

        [JsonProperty("botRot")]
        public long BotRot { get; set; }

        [JsonProperty("topTeam")]
        public string TopTeam { get; set; }

        [JsonProperty("botTeam")]
        public string BotTeam { get; set; }

        [JsonProperty("topTeamShort")]
        public string TopTeamShort { get; set; }

        [JsonProperty("botTeamShort")]
        public string BotTeamShort { get; set; }

        [JsonProperty("linesActive")]
        public bool LinesActive { get; set; }

        [JsonProperty("lines")]
        public lines[] Lines { get; set; }

        [JsonProperty("situation")]
        public Situation Situation { get; set; }

        [JsonProperty("isBoard")]
        public bool IsBoard { get; set; }

        [JsonProperty("debug")]
        public string Debug { get; set; }
        }

        public partial class lines
        {
            [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("hold")]
        public long Hold { get; set; }

        [JsonProperty("sliderHold")]
        public long SliderHold { get; set; }

        [JsonProperty("lineId")]
        public Guid LineId { get; set; }

        [JsonProperty("handicap")]
        public double Handicap { get; set; }

        [JsonProperty("odd")]
        public double Odd { get; set; }

        [JsonProperty("suspended")]
        public bool Suspended { get; set; }

        [JsonProperty("propName")]
        public string PropName { get; set; }

        [JsonProperty("propType")]
        public long PropType { get; set; }
        }

        public partial class Situation
        {
        [JsonProperty("situationId")]
        public string SituationId { get; set; }

        [JsonProperty("topScore")]
        public long TopScore { get; set; }

        [JsonProperty("botScore")]
        public long BotScore { get; set; }

        [JsonProperty("quarter")]
        public long Quarter { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("wBall")]
        public string WBall { get; set; }

        [JsonProperty("down")]
        public long Down { get; set; }

        [JsonProperty("distance")]
        public long Distance { get; set; }

        [JsonProperty("yardline")]
        public string Yardline { get; set; }
        }
    }
}