using Nop.Core.Configuration;

namespace Nop.Plugin.Widgets.NewsmanRemarketing
{
    public class NewsmanRemarketingSettings : ISettings
    {
        public string NewsmanRemarketingId { get; set; }
        public bool EnableEcommerce { get; set; }
        public string WidgetZone { get; set; }
    }
}