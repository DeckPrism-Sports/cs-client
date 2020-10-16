#region Usings
using DPSports.Configuration;
using DPSports.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
#endregion

namespace DPSports.Logging
{
    public static class LogManager
    {
        private static bool _enabledLogger = false;
        private static ILogger _logger;
        private static readonly Dictionary<long, ILogger> _loggerByGameId = new Dictionary<long, ILogger>(50);

        public static void EnableLogger(ILogger logger)
        {
            if(_enabledLogger)
                return;

            _logger = logger;
            _enabledLogger = true;
        }

        public static void Error(string msg, long? gameId, object[] propValues = null)
        {
            Error(null, msg, gameId, propValues);
        }
        public static void Error(Exception ex, string msg, long? gameId, object[] propValues = null)
        {
            if(!_enabledLogger)
                return;

            Log(ex, msg, gameId, propValues, LogEntryType.Error);
        }

        public static void Warning(string msg, long? gameId, object[] propValues = null)
        {
            Warning(null, msg, gameId, propValues);
        }
        public static void Warning(Exception ex, string msg, long? gameId, object[] propValues = null)
        {
            Log(ex, msg, gameId, propValues, LogEntryType.Warning);
        }

        public static void Info(string msg, long? gameId, object[] propValues = null)
        {
            Info(null, msg, gameId, propValues);
        }
        public static void Info(Exception ex, string msg, long? gameId, object[] propValues = null)
        {
            Log(ex, msg, gameId, propValues, LogEntryType.Information);
        }

        private static string _baseTemplate = "{ApplicationName}: ";
        private static void Log(Exception ex, string msg, long? gameId, object[] propValues, LogEntryType type)
        {
            if(!_enabledLogger)
                return;

            var propCount = 0;

            if(propValues != null)
                propCount = propValues.Length;

            //we need a list of values to populate the message template
            var vals = new List<object>(2 + propCount)
            {
                ConfigurationManager.AppSettings.ApplicationName
            };

            var logger = _logger;

            if(gameId.HasValue)
            {
                if(_loggerByGameId.ContainsKey(gameId.Value))
                {
                    logger = _loggerByGameId[gameId.Value];
                }
                else
                {
                    logger = _logger.ForContext("EventId", gameId.Value);
                    _loggerByGameId.Add(gameId.Value, logger);
                }
            }

            msg = _baseTemplate + msg;

            if(propValues?.Length > 0)
                vals.AddRange(propValues);

            var exWrapper = (ExceptionWrapper)null;

            var hasException = ex != null;

            if(hasException)
                exWrapper = new ExceptionWrapper(ex);

            try
            {
                switch(type)
                {
                    case LogEntryType.Information:
                        if(hasException)
                            logger.Information(exWrapper, msg, vals.ToArray());
                        else
                            logger.Information(msg, vals.ToArray());
                        break;
                    case LogEntryType.Error:
                        if(hasException)
                            logger.Error(exWrapper, msg, vals.ToArray());
                        else
                            logger.Error(msg, vals.ToArray());
                        break;
                    case LogEntryType.Warning:
                        if(hasException)
                            logger.Warning(exWrapper, msg, vals.ToArray());
                        else
                            logger.Warning(msg, vals.ToArray());
                        break;
                    case LogEntryType.SuccessAudit:
                    case LogEntryType.FailureAudit:
                    default:
                        if(hasException)
                            logger.Information(exWrapper, msg, vals.ToArray());
                        else
                            logger.Information(msg, vals.ToArray());
                        break;
                }
            }
            catch //(Exception ex)
            {
            }
        }
        public static void CloseAndFlush()
        {
            Serilog.Log.CloseAndFlush();
        }
    }
}