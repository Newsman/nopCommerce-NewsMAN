using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Newsman.Models
{
    public class Customer
    {
        #region Ctor

        #endregion

        #region Properties

        public string email { get; set; }

        public string first_name { get; set; }

        public string last_name { get; set; }

        #endregion
    }
}