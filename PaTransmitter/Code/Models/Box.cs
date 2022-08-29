namespace PaTransmitter.Code.Models
{
    /// <summary>
    /// Trasmitter universal message type used internally at the moment.
    /// </summary>
    public partial class Box
    {
        /// <summary>
        /// The service where the user entered this message.
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// Channel used by user in origin service.
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// User ID. Depending on the origin, we can determine which system
        /// the identifier belongs to (example, gamechat -> uber).
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Username used in chat service (game or discord).
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Text content of message.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Timestamp of the original message.
        /// </summary>
        public DateTime? TimeStamp { get; set; }
    }


    public partial class Box
    {
        public static Box CreateBox(string origin, string channel, string userId, string userName, string text, DateTime? timeStamp)
        {
            return new Box()
            {
                Origin = origin,
                Channel = channel,
                UserId = userId,
                UserName = userName,
                Text = text,
                TimeStamp = timeStamp
            };
        }
    }

    public partial class Box
    {
        /// <summary>
        /// Origin constants and functions. 
        /// </summary>
        public class Origins
        {
            public const string GameChat = "game_c";
            public const string GameChatAdministration = "game_c_admin";

            public const string InternationalDiscord = "int_d";

            public const string RussianDiscord = "ru_d";
            public const string FrenchDiscord = "fr_d";

            public static string GetFullOrigin(string shortOrigin)
            {
                string fullOrigin = shortOrigin switch
                {
                    "game_c" => "Game Chat",
                    "int_d" => "International Discord",
                    "ru_d" => "Russian Discord",
                    "fr_d" => "French Discord",
                    _ => "Unknown origin"
                };

                return fullOrigin;
            }
        }
    }
}
