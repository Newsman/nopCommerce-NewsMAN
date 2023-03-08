using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Newsman.Models
{
    /// <summary>
    /// Represents Newsman configuration model
    /// </summary>
    public record ConfigurationModel
    {
        #region Ctor

        public ConfigurationModel()
        {
            AvailableLists = new List<SelectListItem>();
            AvailableSegments = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        public int ActiveStoreScopeConfiguration { get; set; }

        [Display(Name = "Api Key")]
        [DataType(DataType.Text)]
        public string ApiKey { get; set; }

        [Display(Name = "User Id")]
        public string UserId { get; set; }

        [Display(Name = "Import Type")]
        public string ImportType { get; set; }
        public IList<SelectListItem> AvailableImportTypes
        {
            get
            {
                return new List<SelectListItem>() {
            new SelectListItem("Subscribers", "subscribers"),
            new SelectListItem("Subscribers and Customers", "subscribersCustomers")
        };
            }
        }

        [Display(Name = "Allow Api")]
        public string AllowApi { get; set; }
        public IList<SelectListItem> AvailableApi
        {
            get
            {
                return new List<SelectListItem>() {
            new SelectListItem("Enabled", "enabled"),
            new SelectListItem("Disabled", "disabled")
        };
            }
        }

        [Display(Name = "List")]
        public string ListId { get; set; }
        public bool ListId_OverrideForStore { get; set; }
        public IList<SelectListItem> AvailableLists { get; set; }

        [Display(Name = "Segment")]
        public string SegmentId { get; set; }
        public bool SegmentId_OverrideForStore { get; set; }
        public IList<SelectListItem> AvailableSegments { get; set; }

        #endregion
    }
}