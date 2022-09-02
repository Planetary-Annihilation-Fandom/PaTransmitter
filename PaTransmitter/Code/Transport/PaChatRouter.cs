using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.SignalR;
using PaFandom.Code.Types;
using PaTransmitter.Code.Models;
using PaTransmitter.Code.Models.Community;
using System.Collections.Concurrent;

namespace PaTransmitter.Code.Transport
{
    /// <summary>
    /// Responsible for communication with the game chat.
    /// </summary>
    public class PaChatRouter
    {
        private const int LoopDelayMilliseconds = 350;

        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _apiUrl;

        /// <summary>
        /// Connection to game chat server.
        /// </summary>
        private HubConnection? _connection;
        /// <summary>
        /// Interface for send and receive messages.
        /// </summary>
        private IHubProxy? _hub;

        /// <summary>
        /// Messages needs to send.
        /// </summary>
        private readonly ConcurrentQueue<ExternalMessage> _messageQueue;

        public ILogger Logger { get; init; }

        /// <summary>
        /// Fires when new message appear in pachat. Box - program internal message format.
        /// </summary>
        public event Action<Box> OnBox = delegate { };
        public event Action<Box, ulong> OnDirectBoxFromAdministration = delegate { };

        public PaChatRouter(string apiKey, string apiSecret, string apiUrl)
        {
            _messageQueue = new ConcurrentQueue<ExternalMessage>();

            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _apiUrl = apiUrl;

            // Run infinity loop that checks messages and sends them.
            Task.Run(SendMessagesLoop);
        }

        /// <summary>
        /// Loop for sending messages.
        /// </summary>
        private async Task SendMessagesLoop()
        {
            try
            {
                // Send messages with interval if connection exist. Otherwise 
                while (true)
                {
                    // checking for null and property
                    if(_connection is { State: ConnectionState.Connected })
                    {
                        if (!_messageQueue.IsEmpty)
                        {
                            ExternalMessage message;
                            var success = _messageQueue.TryDequeue(out message);
                            if (success)
                                await SendMessageInternal(message);
                        }
                        // Make delay to reduce cpu usage.
                        await Task.Delay(LoopDelayMilliseconds);
                        continue;
                    }
                    // Make delay to reduce cpu usage doubled if no messages sent.
                    await Task.Delay(LoopDelayMilliseconds*2);
                }
            }
            catch(Exception ex)
            {
                Logger.LogError("[PaRouter] Exception in send loop: {Exception}", ex.Message);
            }
        }

        /// <summary>
        /// Connecting to a game chat server.
        /// </summary>
        public async Task ConnectToApi()
        {
            // If already exist.
            if (_connection != null || _hub != null)
                throw new InvalidOperationException();

            _connection = new HubConnection(_apiUrl);
            // Auth
            _connection.Headers.Add("ApiKey", _apiKey);
            _connection.Headers.Add("ApiSecret", _apiSecret);
            // Events
            _connection.Reconnecting += OnReconnectingToPachat;
            _connection.Reconnected += OnReconnectedToPachat;
            _connection.Closed += OnConnectionClosed;
            _connection.Error += OnConnectionError;

            // Handle messages from server.
            // SendMessage is an event name defined on PaChat side.
            _hub = _connection.CreateHubProxy("chathub");
            _hub.On<string, Message>("SendMessage", ReciveMessage);

            await _connection.Start();
        }

        private void OnConnectionError(Exception obj)
        {
            Logger.LogError("{ConnectionError}", obj.Message);
        }

        private void OnConnectionClosed()
        {
            Logger.LogError("Connection closed");
            ReconnectToPachatManually();
        }

        private void OnReconnectedToPachat()
        {
            Logger.LogWarning("Successfully reconnected to server");
        }

        private void OnReconnectingToPachat()
        {
            Logger.LogWarning("Standard reconnecting to server");
        }

        /// <summary>
        /// Start reconnection with recreating connection and hub.
        /// </summary>
        private void ReconnectToPachatManually()
        {
            _connection = null;
            _hub = null;
            // TODO: продолжить рефакторинг
            Logger.LogWarning("Trying reconnect manually!");

            ConnectToApi();
        }

        /// <summary>
        /// Send messages to pachat.
        /// </summary>
        public void SendMessage(ExternalMessage message)
        {
            // Adding message to queue.
            _messageQueue.Enqueue(message);
        }

        private async Task SendMessageInternal(ExternalMessage msg)
        {
            // Call server method with message argument.
            await _hub.Invoke("PushExternalMessage", "Global",msg);
        }

        private void ReciveMessage(string channel, Message message)
        {
            // If we send this message.
            if (message.Source == _apiKey)
                return;

            // If server specify user. Obviously this used when server spam protecting.
            if (!string.IsNullOrWhiteSpace(message.TargetUserId))
            {
                var box = Box.CreateBox(Box.Origins.GameChatAdministration, "none", "none",
                    CommunityConsts.GameChatAdministrator, message.Text, DateTime.Now);

                OnDirectBoxFromAdministration?.Invoke(box, ulong.Parse(message.TargetUserId));
                return;
            }

#if DEBUG
            Logger.LogInformation(
                $"pachat - {message.TimeStamp.Value.Hour}:{message.TimeStamp.Value.Minute} {message.PlayerName}: {message.Text} : source -{message.Source}");

#endif

            OnBox?.Invoke(Box.CreateBox(Box.Origins.GameChat, message.ChannelName, message.UberId, message.PlayerName, message.Text, message.TimeStamp));
        }
    }
}
