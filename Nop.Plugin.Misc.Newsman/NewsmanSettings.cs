using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.Newsman
{
    /// <summary>
    /// Represents MailChimp plugin settings
    /// </summary>
    public class NewsmanSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the User Id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the API key
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the Import Type
        /// </summary>
        public string ImportType { get; set; }

        public string AllowApi { get; set; }

        /// <summary>
        /// Gets or sets identifier of user list
        /// </summary>
        public string ListId { get; set; }

        /// <summary>
        /// Gets or sets identifier of user list
        /// </summary>
        public string SegmentId { get; set; }
    }
}