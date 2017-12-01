using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.Models;

namespace Nop.Plugin.Tax.Avalara.Models.Tax
{
    /// <summary>
    /// Represents a tax category list model
    /// </summary>
    public class TaxCategoryListModel : BaseNopModel
    {
        #region Ctor

        public TaxCategoryListModel()
        {
            TaxCodeTypes = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        public IList<SelectListItem> TaxCodeTypes { get; set; }

        #endregion
    }
}