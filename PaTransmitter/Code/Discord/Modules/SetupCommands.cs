using Discord.Commands;
using PaTransmitter.Code.Models;
using PaTransmitter.Code.Transport;

namespace PaTransmitter.Code.Discord.Modules
{
    /// <summary>
    /// Discord bot commands.
    /// </summary>
    [Group("transmit")]
    public class SetupCommands : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task Ping()
        {
            await ReplyAsync("ping recieved");
        }

        [Command("help")]
        public async Task Help()
        {
            await ReplyAsync($"{TransmitNodeConsts.EndpointChannelGlobal} and {TransmitNodeConsts.EndpointChannelFindGame}");
        }

        /// <summary>
        /// Create new transmitter node. Example - !transmit setnode general global.
        /// </summary>
        [Command("setnode")]
        public async Task SetNode(string channel, string endChannel, byte nodeOption)
        {
            var serverId = Context.Guild.Id;
            var channelId = Context.Guild.Channels.FirstOrDefault(x => x.Name == channel)?.Id;

            if(channelId == null)
            {
                await ReplyAsync($"Channel {channel} not found");
                return;
            }

            try
            {
                var node = new TransmitNode(serverId, channelId.Value, endChannel, channel, (NodeOption)nodeOption);
                TransportsManager.Instance.SetNode(node);
            }
            catch(Exception ex)
            {
                await ReplyAsync(ex.Message);
            }

            await ReplyAsync($"Node {Context.Guild.Name}: {channel} <-> EndServer: {endChannel} has been set!");
        }

        /// <summary>
        /// Delete node from database and runtime list.
        /// </summary>
        [Command("deletenode")]
        public async Task DeleteNode(string channel)
        {
            var serverId = Context.Guild.Id;
            var channelId = Context.Guild.Channels.FirstOrDefault(x => x.Name == channel)?.Id;

            if (channelId == null)
            {
                await ReplyAsync($"Channel {channel} not found");
                return;
            }

            TransportsManager.Instance.DeleteNode(channelId.Value);
            await ReplyAsync("Node has removed");
        }

        [Command("listnodes")]
        public async Task ListNodes()
        {
            var nodes = TransportsManager.Instance.ReadonlyNodes;

            if(nodes.Count == 0)
            {
                await ReplyAsync($"Nodes are not set :(");
                return;
            }

            for(var i = 0; i< nodes.Count; i++)
            {
                var node = nodes[i];
                await ReplyAsync($"Node[{i}] - {node.ChannelName} from {node.ServerId}.");
            }
        }
    }
}
