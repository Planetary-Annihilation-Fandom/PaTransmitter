namespace PaTransmitter.Code.Models
{
    /// <summary>
    /// River`s type for sending messages to pachat server.
    /// </summary>
    public class ExternalMessage
    {
        public string PlayerId { get; set; }
        public string Text { get; set; }
        public string PlayerName { get; set; }
    }

    /// <summary>
    /// River`s message type used on pachat server.
    /// </summary>
    public class Message
    {
        public Guid? Id { get; set; }
        public string UberId { get; set; }
        public string Text { get; set; }
        public DateTime? TimeStamp { get; set; }
        public string PlayerName { get; set; }
        public string ChannelName { get; set; }
        public string Source { get; set; }
        public string TargetUserId { get; set; }
    }
}
