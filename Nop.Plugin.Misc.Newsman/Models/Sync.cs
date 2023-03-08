using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Newsman.Models
{
    public class SyncModel
    {
        #region Ctor

        #endregion

        #region Properties

        public string email { get; set; }

        public string firstname { get; set; }

        public string lastname { get; set; }

        public string tel { get; set; }

        #endregion
    }
}