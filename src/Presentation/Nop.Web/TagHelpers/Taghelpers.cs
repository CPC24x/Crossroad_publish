// panorazzi

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LinqToDB.Common;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Models.Catalog;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Nop.Web.TagHelpers

{
    [HtmlTargetElement(Attributes = "hide-empty")]
    public class HideEmptyTagHelper : TagHelper
    {
        // This is the attribute to set in your view
        public string HideEmpty { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Check if the value is null or empty
            if (string.IsNullOrWhiteSpace(HideEmpty))
            {
                // Suppress output if empty
                output.SuppressOutput();
            }
        }
    }
}