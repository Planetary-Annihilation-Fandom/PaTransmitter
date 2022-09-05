using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PaFandom.Code.Types;
using PaTransmitter.Code.Discord;

namespace PaTransmitter.Code.Transport
{
    /// <summary>
    /// Responsible for connecting to Discord.
    /// </summary>
    public class DiscordBotRouter
    {
        private readonly string _token;

        /// <summary>
        /// Connection to discord server.
        /// </summary>
        private DiscordSocketClient? _client;

        // Not used externally, but i cached it for GC clean avoiding purpose
        private Option<CommandService> _commandService;
        private Option<CommandHandler> _commandHandler;
        
        /// <summary>
        /// How to post message in discord channels. Formatting.
        /// </summary>
        private readonly DiscordFunctions.ColorScheme _colorScheme;

        public ILogger Logger { get; init; }

        /// <summary>
        /// Fires event on message received from discord channel.
        /// </summary>
        public event Action<SocketMessage> Received = delegate { };

        public DiscordBotRouter(string token)
        {
            _token = token;
            _colorScheme = DiscordFunctions.ColorScheme.Blue;
        }

        /// <summary>
        /// Initializes the discord client and events.
        /// </summary>
        public async Task ConnectToApi()
        {
            if (_client != null)
            {
                if(_client.ConnectionState == ConnectionState.Connected)
                    return;
                
                Received = delegate {};
                
                await _client.DisposeAsync();
                _client = null;
            }
            
            _client = new DiscordSocketClient();
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            _client.Ready += InitializeCommandsService;
            _client.Ready += async () => Logger.LogInformation("[Discord] Connection to discord is ready");
            _client.MessageReceived += RethrowMessage;

            await Task.Delay(-1);
        }
        
        /// <summary>
        /// Initialize command handler and service.
        /// </summary>
        private async Task InitializeCommandsService()
        {
            var commandService = new CommandService();
            var commandHandler = new CommandHandler(_client, commandService);

            _commandService = commandService;
            _commandHandler = commandHandler;

            await commandHandler.InstallCommandsAsync();
        }

        private Task RethrowMessage(SocketMessage msg)
        {
            // if author is This bot
            if (msg.Author.Id == _client.CurrentUser.Id)
                return Task.CompletedTask;

            // if message is command
            if (msg.Content[0] == '!')
                return Task.CompletedTask;

            Received(msg);
            return Task.CompletedTask;
        }

        public async Task SendChat(ulong serverId, ulong channelId, string username, string text)
        {
            await SendChat(serverId, channelId, username, text, _colorScheme);
        }

        public async Task SendDirect(ulong userId,string username, string text)
        {
            var user = _client.GetUser(userId);
            try
            {
                await user.SendMessageAsync(DiscordFunctions.FormatColorScheme(username, text,
                    DiscordFunctions.ColorScheme.Orange));
            }
            // If user blocked bot or something else like that.
            catch (HttpException)
            {
                Logger.LogWarning("[Discord] User {UserUsername} cannot accept direct message", user.Username);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex.Message);
            }
        }
        
        private async Task SendChat(ulong serverId, ulong channelId, string username, string text, DiscordFunctions.ColorScheme colorScheme)
        {
            await _client
                .GetGuild(serverId)
                .GetTextChannel(channelId)
                .SendMessageAsync(DiscordFunctions.FormatColorScheme(username, text, colorScheme));
        }
        
        public static class DiscordFunctions
        {
            public static string FormatColorScheme(string username, string text, ColorScheme scheme)
            {
                var formattedText = scheme switch
                {
                    ColorScheme.Blue => $"```ini\n [{username}] {text}\n```",
                    ColorScheme.Orange => $"```css\n [{username}] {text}\n```",
                    _ => $"[{username}] {text}"
                };
                return formattedText;
            }

            public enum ColorScheme
            {
                Blue, // ```ini [___] ___```
                Orange, // ```css [___] ___``` 
            }
        }
    }
}
