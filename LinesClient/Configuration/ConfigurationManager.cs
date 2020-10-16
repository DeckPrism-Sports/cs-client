using Microsoft.Extensions.Configuration;
using System;

namespace DPSports.Configuration
{
    public static class ConfigurationManager
    {
        public static IConfigurationRoot Configuration { get; set; }

        public static class AppSettings
        {
            public static string ApplicationName => GetValue<string>("AppSettings", "ApplicationName");
        }

        public static class Rabbit
        {
            private static readonly string _rabbitUser = GetValue<string>("Rabbit", "User");
            private static readonly string _rabbitPassword = GetValue<string>("Rabbit", "Password");
            private static readonly string _rabbitHost = GetValue<string>("Rabbit", "Host");
            private static readonly string _rabbitVHost = GetValue<string>("Rabbit", "VHost");
            private static readonly string _rabbitPrefix = GetValue<string>("Rabbit", "Prefix");
            public static string RabbitURI => $"{_rabbitPrefix}://{_rabbitUser}:{_rabbitPassword}@{_rabbitHost}/{_rabbitVHost}?heartbeat=15";
            public static string RabbitRoutingKey => GetValue<string>("Rabbit", "RoutingKey");
            public static string RabbitExchange => GetValue<string>("Rabbit", "Exchange");
            public static string OutExchange => GetValue<string>("Rabbit", "OutExchange");
        }

        public static class MainApi
        {
            public static string Host => GetValue<string>("Api", "Host");         
            public static string ApiKey => GetValue<string>("Api", "ApiKey");
            public static int Attempts => GetValue<int>("Api", "Attempts");
        }

        private static T GetValue<T>(string configSection, string keyName)
        {
            return (T)Convert.ChangeType(Configuration[$"{configSection}:{keyName}"], typeof(T));
        }
    }
}
