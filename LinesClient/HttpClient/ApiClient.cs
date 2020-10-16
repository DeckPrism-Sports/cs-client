#region Usings
using DPSports.Configuration;
using DPSports.Entity.DTO;
using DPSports.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
#endregion

namespace DPSports.Feed
{
    public class ApiClient : IApiClient
    {
        private readonly string _host;     
        private readonly string _apiKey;
        private readonly HttpClient _client;
        private readonly int _retries;
        private const string _responseFormat = "application/json";


        private readonly JsonSerializerSettings _defaultJsonConf = new JsonSerializerSettings()
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Error = (sender, errorArgs) =>
            {
                var currentError = errorArgs.ErrorContext.Error.Message;
                var path = errorArgs.ErrorContext.Path;
                errorArgs.ErrorContext.Handled = true;
                // Console.WriteLine($"Error:{currentError} Path:{path}"); // Commenting out JsonDeserializer Error Console Writeline to have less verbose console. Uncomment for testing.
            },
        };

        public ApiClient()
        {
            _host = ConfigurationManager.MainApi.Host;            
            _apiKey = ConfigurationManager.MainApi.ApiKey;
            _retries = ConfigurationManager.MainApi.Attempts;

            _client = new HttpClient { BaseAddress = new Uri(_host), Timeout = new TimeSpan(0, 0, 0, 0, -1) };

            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_responseFormat));
        }

        public async Task<List<Game>> GetGames(DateTime? gamesStart, DateTime? gamesEnd)
        {
            try
            {               
                var jsonString = await CallLinesApi(gamesStart, gamesEnd);

                if(string.IsNullOrWhiteSpace(jsonString))
                    return new List<Game>(0);

                var callResult = JsonConvert.DeserializeObject<DataResult<List<Game>>>(jsonString, _defaultJsonConf);

                if(!callResult.Success)
                    return new List<Game>(0);

                return callResult.Content;
            }
            catch(Exception ex)
            {
                LogManager.Error(ex, $"Failed to process result from lines api", null);
                throw ex;
            }
        }

        private async Task<string> CallLinesApi(DateTime? gamesStart, DateTime? gamesEnd)
        {
            var startString = gamesStart.HasValue ? gamesStart.Value.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd");
            var endString = gamesEnd.HasValue ? gamesEnd.Value.ToString("yyyy-MM-dd") : DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
            var url = $"{_host}/api/Lines/{Sports.ALL}/{startString}/{endString}?apiKey={_apiKey}";

            for(int i = 0; i < _retries; i++)
            {
                try
                {
                    LogManager.Info("Calling Lines Api: " + url, null);
                    return await _client.GetStringAsync(url);
                }
                catch(Exception ex)
                {
                    if(i < _retries - 1)
                        continue;

                    LogManager.Error(ex, $"Error calling lines api. Attempts: {i + 1}" + url, null);
                }
                break;
            }
            return "";
        }
    }
}