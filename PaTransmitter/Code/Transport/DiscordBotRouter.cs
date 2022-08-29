using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PaFandom.Code.Types;
using PaTransmitter.Code.Discord;
using PaTransmitter.Code.Models;

namespace PaTransmitter.Code.Transport
{
    /// <summary>
    /// Responsible for connecting to Discord.
    /// </summary>
    public class DiscordBotRouter
    {
        private readonly string Token;

        /// <summary>
        /// Connection to discord server.
        /// </summary>
        private Option<DiscordSocketClient> _client;

        // Not used externaly, but i cached it for GC clean avoiding purpose
        private Option<CommandService> _commandService;
        private Option<CommandHandler> _commandHandler;

        public ILogger Logger { get; set; }

        /// <summary>
        /// How to post message in discord channels. Formatting.
        /// </summary>
        public DiscordFunctions.ColorScheme ColorScheme { get; set; }

        /// <summary>
        /// Fires event on message recieved from discord channel.
        /// </summary>
        public event Action<SocketMessage> OnMessage = delegate { };

        public DiscordBotRouter(string token)
        {
            Token = token;
            ColorScheme = DiscordFunctions.ColorScheme.Blue;
        }

        /// <summary>
        /// Initializes the discord client and events.
        /// </summary>
        public async Task ConnectToDiscordApi()
        {
            if(_client)
                throw new InvalidOperationException();

            _client = new DiscordSocketClient();
            await _client.option.LoginAsync(TokenType.Bot, Token);
            await _client.option.StartAsync();

            _client.option.Ready += OnClientReady;

            await Task.Delay(-1);
        }

        private async Task HandleMessage(SocketMessage msg)
        {
            if (msg.Author.Id == _client.option.CurrentUser.Id)
                return;

            if (msg.Content[0] == '!')
                return;

            Logger.LogInformation($"discord - {msg.Author.Username}: {msg.Content} - channel id = {msg.Channel.Id}");
            OnMessage?.Invoke(msg);
        }

        public async Task SendMessage(ulong serverId, ulong channelId, string username, string text)
        {
            await SendMessage(serverId, channelId, username, text, ColorScheme);
        }

        public async Task SendMessage(ulong serverId, ulong channelId, string username, string text, DiscordFunctions.ColorScheme colorSheme)
        {
            await _client.option.GetGuild(serverId).GetTextChannel(channelId).SendMessageAsync(DiscordFunctions.FormatColorScheme(username, text, colorSheme));
        }

        public async Task SendDirectMessage(ulong userId,string username, string text, DiscordFunctions.ColorScheme colorSheme)
        {
            var user = _client.option.GetUser(userId);
            try
            {
                await UserExtensions.SendMessageAsync(user, DiscordFunctions.FormatColorScheme(username, text, colorSheme));

            }
            // If user blocked bot or something else like that.
            catch (Exception ex)
            {
                Logger.LogWarning(ex.Message);
            }
        }

        /// <summary>
        /// Initialize command handler and service.
        /// </summary>
        private async Task OnClientReady()
        {
            _client.option.MessageReceived += HandleMessage;

            var commandService = new CommandService();
            var commandHandler = new CommandHandler(_client, commandService);

            _commandService = commandService;
            _commandHandler = commandHandler;

            await commandHandler.InstallCommandsAsync();
        }
    }

    public class DiscordFunctions
    {
        public static string FormatColorScheme(string username, string text, ColorScheme scheme = ColorScheme.Orange)
        {
            var formattedText = scheme switch
            {
                ColorScheme.Blue => $"```ini\n [{username}] {text}\n```",
                ColorScheme.Orange => $"```css\n [{username}] {text}\n```",
                _ => throw new NotImplementedException()
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
