using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Newsman.Models
{
    public class SegmentsModel
    {
        #region Ctor

        #endregion

        #region Properties

        public string segment_id { get; set; }

        public string segment_name { get; set; }

        #endregion
    }
}