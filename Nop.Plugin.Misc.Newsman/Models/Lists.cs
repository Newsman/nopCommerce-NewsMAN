using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Newsman.Models
{
    public class ListsModel
    {
        #region Ctor

        #endregion

        #region Properties

        public string list_name { get; set; }

        public string list_id { get; set; }

        public string list_type { get; set; }

        #endregion
    }
}