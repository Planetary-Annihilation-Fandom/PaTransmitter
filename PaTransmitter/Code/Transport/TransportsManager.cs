using System.Collections.Immutable;
using Discord.WebSocket;
using PaFandom.Code.Types;
using PaTransmitter.Code.Models;
using PaTransmitter.Code.Services;
using PaTransmitter.Code.Services.Databases.Contexts;

namespace PaTransmitter.Code.Transport
{
    /// <summary>
    /// Responsible for the connection of text channels from different systems 
    /// </summary>
    [LazyInstance(false)]
    public class TransportsManager : Singleton<TransportsManager>
    {
        /// <summary>
        /// Responsible for receiving and transmitting messages from the Game chat server.
        /// </summary>
        private PaChatRouter? _paChatRouter;
        /// <summary>
        /// Responsible for receiving and transmitting messages from the Discord server.
        /// </summary>
        private DiscordBotRouter? _discordBotRouter;

        /// <summary>
        /// Database EntityFramework interface.
        /// </summary>
        private TransmitNodeContext? _nodesDb;

        public IReadOnlyList<TransmitNode> ReadonlyNodes => _nodesDb?.Nodes.ToImmutableList() ??
                                                            ImmutableList<TransmitNode>.Empty;

        public Task InitializeTransports(WebApplication app)
        {
            // If already exist.
            if (_paChatRouter!=null && _discordBotRouter!=null)
            {
                SetupRoutes();
                return Task.CompletedTask;
            }

            var logger = app.Logger;

            // Getting keys.
            var file = FileManager.Instance;

            logger.LogInformation("Trying to read credentials...");

            var apikey = file.ReadFile("/credentials/pachat/apikey.txt");
            var apisecret = file.ReadFile("/credentials/pachat/apisecret.txt");
            var signalrUrl = file.ReadFile("/credentials/pachat/signalrUrl.txt");

            var discordToken = file.ReadFile("/credentials/discord/token.txt");

            // If any key is empty close app and notify user to set these credentials.
            if (!discordToken || !apikey || !apisecret || !signalrUrl)
            {
                logger.LogError($"Application initialized folder structure. You need to restart app after setup credentials in file at: {file.Content} !");
                app.Lifetime.StopApplication();
                return Task.CompletedTask;
                //throw new Exception("Setup credentials in" + file.Content);
            }

            logger.LogInformation("Credentials has read");

            _paChatRouter = new PaChatRouter(apikey, apisecret, signalrUrl)
            { Logger = logger };

            _discordBotRouter = new DiscordBotRouter(discordToken)
            { Logger = logger };

            SetupRoutes();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Add new node to database and instantly to list.
        /// </summary>
        public void SetNode(TransmitNode node)
        {
            if (_nodesDb == null)
                throw new InvalidOperationException();
            
            _nodesDb.Add(node);
            _nodesDb.SaveChanges();
        }

        /// <summary>
        /// Delete node from db and instantly from list.
        /// </summary>
        public void DeleteNode(ulong channelId)
        {
            if (_nodesDb == null)
                throw new InvalidOperationException();
            
            var node = _nodesDb.Nodes.FirstOrDefault(x=>x.ChannelId == channelId);
            if(node != null)
            {
                _nodesDb.Nodes.Remove(node);
                _nodesDb.SaveChanges(true);
            }
            
        }

        /// <summary>
        /// Initialize db and message event handlers.
        /// </summary>
        private void SetupRoutes()
        {
            Task.Run(_paChatRouter!.ConnectToApi);
            Task.Run(_discordBotRouter!.ConnectToApi);
            
            _nodesDb?.Dispose();
            _nodesDb = new TransmitNodeContext();

            _discordBotRouter.Received += TransmitDiscord;

            _paChatRouter.Received += TransmitPa;
            _paChatRouter.ReceivedFromAdministration += TransmitPaToUser;
        }

        /// <summary>
        /// Forwarding messages from discord to the game.
        /// </summary>
        private void TransmitDiscord(SocketMessage discordMessage)
        {
            if (_nodesDb == null)
                throw new InvalidOperationException();
            
            var serverId = (discordMessage.Channel as SocketGuildChannel)!.Guild.Id;
            var channelId = discordMessage.Channel.Id;
            
            // If node with that server and channel exist.
            var node = _nodesDb.Nodes.FirstOrDefault(x=>x.ServerId == serverId && x.ChannelId == channelId);
            if (node != null)
            {
                if (node.Option == NodeOption.Read)
                    return;

                TransmitDiscordToDiscords(node, discordMessage);

                var externalMsg = new ExternalMessage()
                {
                    PlayerId = discordMessage.Author.Id.ToString(),//discord user id
                    PlayerName = $"{discordMessage.Author.Username}",//discord display name
                    Text = discordMessage.CleanContent,
                };

                _paChatRouter!.PullMessage(externalMsg);
            }
        }

        /// <summary>
        /// Send message to other nodes except one.
        /// </summary>
        private void TransmitDiscordToDiscords(TransmitNode exceptNode,SocketMessage dmsg)
        {
            if (_nodesDb == null)
                throw new InvalidOperationException();
            
            foreach (var node in _nodesDb.Nodes)
            {
                if (node == exceptNode)
                    continue;

                var serverId = node.ServerId;
                var channelId = node.ChannelId;

                var username = dmsg.Author.Username;
                var message = dmsg.CleanContent;

                Task.Run(() => _discordBotRouter!.SendChat(serverId, channelId, username, message));
            }
        }

        /// <summary>
        /// Forwarding messages from the game to all Discord nodes (channels).
        /// </summary>
        private void TransmitPa(Package package)
        {
            if (_nodesDb == null)
                throw new InvalidOperationException();
            
            foreach(var node in _nodesDb.Nodes)
            {
                var serverId = node.ServerId;
                var channelId = node.ChannelId;

                var username = package.UserName;
                var message = package.Text;

                Task.Run(() => _discordBotRouter!.SendChat(serverId, channelId, username, message));
            }
        }

        /// <summary>
        /// Direct message obviously send from Administration.
        /// </summary>
        private void TransmitPaToUser(Package package, ulong targetId)
        {
            Task.Run(() => _discordBotRouter!.SendDirect(targetId, package.UserName, package.Text));
        }
    }
}
