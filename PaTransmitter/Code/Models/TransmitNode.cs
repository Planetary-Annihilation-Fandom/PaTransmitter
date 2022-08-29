using Newtonsoft.Json;
using PaFandom.Code.Types;

namespace PaTransmitter.Code.Models
{
    public class TransmitNode
    {
        /// <summary>
        /// Database member identifier.
        /// </summary>
        public int TransmitNodeId { get; set; }



        /// <summary>
        /// Discord server.
        /// </summary>
        public ulong ServerId { get; set; }
        /// <summary>
        /// Discord server channel.
        /// </summary>
        public ulong ChannelId { get; set; }
        /// <summary>
        /// PaChat channel
        /// </summary>
        public string EndpointChannel { get; set; }
        /// <summary>
        /// Discord channel name. Not necessary.
        /// </summary>
        public string ChannelName { get; set; }
        /// <summary>
        /// Options of how to process this node.
        /// </summary>
        public NodeOption Option { get; set; }

        public TransmitNode(ulong serverId, ulong channelId, string endpointChannel, string channelName, NodeOption option)
        {
            ServerId = serverId;
            ChannelId = channelId;
            EndpointChannel = endpointChannel;
            ChannelName = channelName;
            Option = option;
        }
    }

    public enum NodeOption : byte
    {
        Read = 0,
        ReadAndWrite = 1
    }

    public class TransmitNodeConsts
    {
        public const string EndpointChannelGlobal = "global";
        public const string EndpointChannelFindGame = "findgame";

        public const string DatabaseName = "transmitnodes";
    }
}
