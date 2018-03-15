using System;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;

namespace Nop.Plugin.Tax.Avalara.Models.Log
{
    /// <summary>
    /// Represents a tax transaction log model
    /// </summary>
    public partial class TaxTransactionLogModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Plugins.Tax.Avalara.Log.Message")]
        public string Message { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Log.LogType")]
        public string LogType { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Log.Customer")]
        public int? CustomerId { get; set; }

        public string CustomerEmail { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Log.CreatedDate")]
        public DateTime CreatedDate { get; set; }
    }
}