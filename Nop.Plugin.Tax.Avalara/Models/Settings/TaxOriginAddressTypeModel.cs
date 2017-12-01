using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;

namespace Nop.Plugin.Tax.Avalara.Models.Settings
{
    /// <summary>
    /// Represents a tax origin address type model
    /// </summary>
    public class TaxOriginAddressTypeModel : BaseNopModel
    {
        public string PrecedingElementId { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Fields.TaxOriginAddressType")]
        public int AvalaraTaxOriginAddressType { get; set; }
        public SelectList TaxOriginAddressTypes { get; set; }
    }
}