#region Usings
using DPSports.Configuration;
using DPSports.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace DPSports.Feed
{
    public class RabbitService : IRabbitService
    {
        private static readonly object _syncRoot = new object();
        private IConnection _conn;
        private string _queueName;       

        public async Task ReceiveMessages<T>(string routingKey, Action<T, CancellationToken> receivedMsgAction, CancellationToken token) where T : class
        {
            await Task.Yield();

            var channel = CreateChannel();

            try
            {
                if(string.IsNullOrWhiteSpace(_queueName))
                    _queueName = channel.QueueDeclare().QueueName;

                var consumer = new EventingBasicConsumer(channel);
                var promise = new TaskCompletionSource<string>();

                token.Register(delegate
                {
                    channel.Close();
                });

                consumer.Received += delegate (object sender, BasicDeliverEventArgs e)
                {
                    ProcessIncomingMessage(receivedMsgAction, e.Body, token);
                };

                consumer.Shutdown += delegate (object sender, ShutdownEventArgs e)
                {
                    promise.SetResult(e.ReplyText);
                };

                channel.QueueBind(_queueName, ConfigurationManager.Rabbit.RabbitExchange, routingKey);
                channel.BasicConsume(_queueName, autoAck: true, consumer);

                var result = await promise.Task;

                if(channel.IsOpen)
                    channel.Close();

                _queueName = string.Empty;
            }
            catch(Exception ex)
            {
                LogManager.Error(ex, "routingKey:" + routingKey, null);
                throw ex;
            }
            finally
            {
                if(channel != null)
                    channel.Dispose();
            }
        }

        private void ProcessIncomingMessage<T>(Action<T, CancellationToken> receivedMsgAction, ReadOnlyMemory<byte> messageBody, CancellationToken token) where T : class
        {
            var msg = "";

            try
            {
                msg = Encoding.UTF8.GetString(messageBody.ToArray());

                var result = JsonConvert.DeserializeObject<T>(msg, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    Error = delegate (object o, ErrorEventArgs error)
                    {
                        error.ErrorContext.Handled = true;
                        LogManager.Error(error?.ErrorContext?.Error, "Rabbit Message deserialization error", null);
                    }
                });

                receivedMsgAction(result, token);
            }
            catch(Exception exception)
            {
                LogManager.Error(exception, $"Rabbit {nameof(ProcessIncomingMessage)} failed: " + msg, null);
            }
        }

        #region Channel
        private IModel CreateChannel()
        {
            var connection = CreateConnection();

            try
            {
                var channel = connection.CreateModel();
                channel.ModelShutdown += ModelClosed;
                return channel;
            }
            catch(Exception ex)
            {
                LogManager.Error(ex, "Rabbit Error creating channel", null);
                throw ex;
            }
        }

        private void ModelClosed(object sender, ShutdownEventArgs e)
        {

        }
        #endregion

        #region Connection
        private IConnection CreateConnection()
        {
            try
            {
                if(_conn != null && _conn.IsOpen)
                    return _conn;

                lock(_syncRoot)
                {
                    if(_conn == null || !_conn.IsOpen)
                    {
                        var connectionFactory = new ConnectionFactory
                        {
                            Uri = new Uri(ConfigurationManager.Rabbit.RabbitURI)
                        };

                        _conn = connectionFactory.CreateConnection();
                        _conn.CallbackException += CallbackError;

                        if(_conn is IAutorecoveringConnection autoRecoveryConnection)
                            autoRecoveryConnection.ConnectionRecoveryError += RecoveryError;

                        LogManager.Info("Rabbit Broker Connected to: " + ConfigurationManager.Rabbit.RabbitURI, null);
                    }
                }

                return _conn;
            }
            catch(Exception ex)
            {
                LogManager.Error(ex, "Rabbit Error creating connection", null);
                throw ex;
            }
        }

        private void CallbackError(object sender, CallbackExceptionEventArgs e)
        {
            LogManager.Error(e.Exception, "Rabbit CallbackError: " + string.Join(Environment.NewLine, e.Detail.Select((KeyValuePair<string, object> x) => $"{x.Key}: {x.Value}")), null);
        }

        private void RecoveryError(object sender, ConnectionRecoveryErrorEventArgs e)
        {
            LogManager.Error(e.Exception, "Rabbit RecoveryError", null);
        }
        #endregion
    }
}