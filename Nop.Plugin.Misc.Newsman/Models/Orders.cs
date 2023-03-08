using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Newsman.Models
{
    public class Order
    {
        #region Ctor

        #endregion

        #region Properties

        public int order_no { get; set; }

        public string date { get; set; }

        public string status { get; set; }

        public string lastname { get; set; }

        public string firstname { get; set; }

        public string email { get; set; }

        public string phone { get; set; }

        public string state { get; set; }

        public string city { get; set; }

        public string address { get; set; }

        public decimal discount { get; set; }

        public decimal shipping { get; set; }

        public decimal fees { get; set; }

        public decimal rebates { get; set; }

        public decimal total { get; set; }

        public List<Product> products { get; set; }

        #endregion
    }
}