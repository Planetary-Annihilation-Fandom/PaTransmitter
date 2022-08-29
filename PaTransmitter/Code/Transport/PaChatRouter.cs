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
        public const int LoopDelayMilliseconds = 350;

        private readonly string ApiKey;
        private readonly string ApiSecret;
        private readonly string ApiUrl;

        /// <summary>
        /// Connection to game chat server.
        /// </summary>
        private Option<HubConnection> connection;
        /// <summary>
        /// Interface for send and recieve messages.
        /// </summary>
        private Option<IHubProxy> hub;

        /// <summary>
        /// Messages needs to send.
        /// </summary>
        private ConcurrentQueue<ExternalMessage> messagesToSend = new ConcurrentQueue<ExternalMessage>();

        public ILogger Logger { get; set; }

        /// <summary>
        /// Fires when new message appear in pachat. Box - program internal message format.
        /// </summary>
        public event Action<Box> OnBox = delegate { };
        public event Action<Box, ulong> OnDirectBoxFromAdministration = delegate { };

        public PaChatRouter(string apiKey, string apiSecret, string apiUrl)
        {
            messagesToSend = new ConcurrentQueue<ExternalMessage>();

            ApiKey = apiKey;
            ApiSecret = apiSecret;
            ApiUrl = apiUrl;

            // Run infinity loop that checks messages and sends them.
            Task.Run(Update);
        }

        /// <summary>
        /// Loop for sending messages.
        /// </summary>
        private async Task Update()
        {
            try
            {
                // Send messages with interval if connection exist. Otherwise 
                while (true)
                {
                    if(connection && connection.option.State == ConnectionState.Connected)
                    {
                        if (messagesToSend.Count > 0)
                        {
                            ExternalMessage message;
                            var success = messagesToSend.TryDequeue(out message);
                            if (success)
                                await SendMessageInternal(message);
                        }

                        // Make delay to reduce cpu usage.
                        await Task.Delay(LoopDelayMilliseconds);
                    }

                    // Make delay to reduce cpu usage doubled.
                    await Task.Delay(LoopDelayMilliseconds*2);
                }
            }
            catch(Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Connecting to a game chat server.
        /// </summary>
        public void ConnectToApi()
        {
            // If already exist.
            if (connection || hub)
                throw new InvalidOperationException();

            
            connection = new HubConnection(ApiUrl);
            connection.option.Headers.Add("ApiKey", ApiKey);
            connection.option.Headers.Add("ApiSecret", ApiSecret);

            connection.option.Reconnecting += OnReconnectingToPachat;
            connection.option.Reconnected += OnReconnectedToPachat;

            connection.option.Closed += OnConnectionClosed;
            connection.option.Error += OnConnectionError;

            // Server connection lobby for this program.
            hub = new Option<IHubProxy>(connection.option.CreateHubProxy("chathub"));
            // Handle messages from server.
            hub.option.On<string, Message>("SendMessage", ReciveMessage);

            // Connecting.
            connection.option.Start().Wait();
        }

        private void OnConnectionError(Exception obj)
        {
            Logger.LogError(obj.Message);
        }

        private void OnConnectionClosed()
        {
            Logger.LogError("Connection closed.");
            ReconnectToPachatManually();
        }

        private void OnReconnectedToPachat()
        {
            Logger.LogWarning("Successfully reconnected to server!");
        }

        private void OnReconnectingToPachat()
        {
            Logger.LogWarning("Standart reconnecting to server.");
        }

        /// <summary>
        /// Start reconnection with recreating connection and hub.
        /// </summary>
        private void ReconnectToPachatManually()
        {
            connection = Option<HubConnection>.None;
            hub = Option<IHubProxy>.None;

            Logger.LogWarning("Trying reconnect manually!");

            ConnectToApi();
        }

        /// <summary>
        /// Send messages to pachat.
        /// </summary>
        public void SendMessage(ExternalMessage message)
        {
            // Adding message to queue.
            messagesToSend.Enqueue(message);
        }

        private async Task SendMessageInternal(ExternalMessage msg)
        {
            // Call server method with message argument.
            await hub.option.Invoke("PushExternalMessage", "Global",msg);
        }

        private void ReciveMessage(string channel, Message message)
        {
            // If we send this message.
            if (message.Source == ApiKey)
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
