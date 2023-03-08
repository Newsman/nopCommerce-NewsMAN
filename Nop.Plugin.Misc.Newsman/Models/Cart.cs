using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Newsman.Models
{
    public class Cart
    {
        public int id { get; set; }

        public string name { get; set; }

        public decimal price { get; set; }

        public int quantity { get; set; }
    }
}