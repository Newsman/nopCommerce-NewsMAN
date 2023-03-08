using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Widgets.NewsmanRemarketing.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }
        
        [Display(Name = "Remarketing Id")]
        public string RemarketingId { get; set; }
        public bool RemarketingId_OverrideForStore { get; set; }

        [Display(Name = "Enable Newsman Remarketing")]
        public bool EnableEcommerce { get; set; }
        public bool EnableEcommerce_OverrideForStore { get; set; }
    }
}