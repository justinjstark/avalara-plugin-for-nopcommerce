using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;

namespace Nop.Plugin.Tax.Avalara.Models.Log
{
    /// <summary>
    /// Represents a tax transaction log list model
    /// </summary>
    public partial class TaxTransactionLogListModel : BaseNopModel
    {
        #region Ctor

        public TaxTransactionLogListModel()
        {
            AvailableLogTypes = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        [NopResourceDisplayName("Plugins.Tax.Avalara.Log.Search.CreatedFrom")]
        [UIHint("DateNullable")]
        public DateTime? CreatedFrom { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Log.Search.CreatedTo")]
        [UIHint("DateNullable")]
        public DateTime? CreatedTo { get; set; }
        
        [NopResourceDisplayName("Plugins.Tax.Avalara.Log.Search.LogType")]
        public int LogTypeId { get; set; }

        public IList<SelectListItem> AvailableLogTypes { get; set; }

        #endregion
    }
}