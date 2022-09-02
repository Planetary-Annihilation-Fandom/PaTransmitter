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
        private Option<PaChatRouter> _paChatRouter;
        /// <summary>
        /// Responsible for receiving and transmitting messages from the Discord server.
        /// </summary>
        private Option<DiscordBotRouter> _discordBotRouter;

        /// <summary>
        /// Database EntityFramework interface.
        /// </summary>
        private Option<TransmitNodeContext> _nodesDb;

        /// <summary>
        /// Initialized list of transmit nodes from _nodesDb. 
        /// </summary>
        private List<TransmitNode> Nodes = new List<TransmitNode>();

        public IReadOnlyList<TransmitNode> ReadonlyNodes => Nodes.AsReadOnly();

        public Task InitializeTransports(WebApplication app)
        {
            // If already exist.
            if (_paChatRouter || _discordBotRouter)
                throw new InvalidOperationException();

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

            logger.LogInformation("Credentials has readed.");

            _paChatRouter = new PaChatRouter(apikey, apisecret, signalrUrl)
            { Logger = logger };

            _discordBotRouter = new DiscordBotRouter(discordToken)
            { Logger = logger };

            SetupRoutes();

            Task.Run(_paChatRouter.option.ConnectToApi);
            Task.Run(_discordBotRouter.option.ConnectToDiscordApi);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Add new node to database and instantly to list.
        /// </summary>
        public void SetNode(TransmitNode node)
        {
            Nodes.Add(node);
            _nodesDb.option.Add(node);
            _nodesDb.option.SaveChanges();
        }

        /// <summary>
        /// Delete node from db and instantly from list.
        /// </summary>
        public void DeleteNode(ulong channelId)
        {
            Nodes.RemoveAll(x => x.ChannelId == channelId);

            var node = _nodesDb.option.Nodes.FirstOrDefault(x=>x.ChannelId == channelId);
            if(node != null)
            {
                _nodesDb.option.Nodes.Remove(node);
                _nodesDb.option.SaveChanges(true);
            }
            
        }

        /// <summary>
        /// Initialize db and message event handlers.
        /// </summary>
        private void SetupRoutes()
        {
            var db = new TransmitNodeContext();
            _nodesDb = db;
            Nodes = _nodesDb.option.Nodes.ToList();

            _discordBotRouter.option.MessageReceived += TransmitDiscordToPA;

            _paChatRouter.option.Received += TransmitPAToDiscord;
            _paChatRouter.option.ReceivedFromAdministration += TransmitPAToDiscordUser;
        }

        /// <summary>
        /// Forwarding messages from discord to the game.
        /// </summary>
        private void TransmitDiscordToPA(SocketMessage dmsg)
        {
            var serverId = (dmsg.Channel as SocketGuildChannel).Guild.Id;
            var channelId = dmsg.Channel.Id;
            
            // If node with that server and channel exist.
            var node = Nodes.FirstOrDefault(x=>x.ServerId == serverId && x.ChannelId == channelId);
            if (node != null)
            {
                if (node.Option == NodeOption.Read)
                    return;

                TransmitDiscordToDiscords(node, dmsg);

                var externalMsg = new ExternalMessage()
                {
                    PlayerId = dmsg.Author.Id.ToString(),//discord user id
                    PlayerName = $"{dmsg.Author.Username}",//discord displayname
                    Text = dmsg.CleanContent,
                };

                _paChatRouter.option.PullMessage(externalMsg);
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// Send message to other nodes except one.
        /// </summary>
        private void TransmitDiscordToDiscords(TransmitNode exceptNode,SocketMessage dmsg)
        {
            foreach (var node in Nodes)
            {
                if (node == exceptNode)
                    continue;

                var serverId = node.ServerId;
                var channelId = node.ChannelId;

                var username = dmsg.Author.Username;
                var message = dmsg.CleanContent;

                Task.Run(() => _discordBotRouter.option.SendMessageToChat(serverId, channelId, username, message));
            }
        }

        /// <summary>
        /// Forwarding messages from the game to all Discord nodes (channels).
        /// </summary>
        private void TransmitPAToDiscord(Package package)
        {
            foreach(var node in Nodes)
            {
                var serverId = node.ServerId;
                var channelId = node.ChannelId;

                var username = package.UserName;
                var message = package.Text;

                Task.Run(() => _discordBotRouter.option.SendMessageToChat(serverId, channelId, username, message));
            }
        }

        /// <summary>
        /// Direct message obviously sended from Administration.
        /// </summary>
        private void TransmitPAToDiscordUser(Package package, ulong targetId)
        {
            Task.Run(() => _discordBotRouter.option.SendMessageToDirect(targetId, package.UserName, package.Text));
        }
    }
}
