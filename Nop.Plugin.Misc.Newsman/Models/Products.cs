using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Newsman.Models
{
    public class Product
    {
        #region Ctor

        #endregion

        #region Properties

        public int id { get; set; }

        public string name { get; set; }

        public int stock_quantity { get; set; }

        public decimal price { get; set; }

        public decimal price_old { get; set; }

        public string image_url { get; set; }

        public string url { get; set; }

        #endregion
    }
}