using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Security;
using Nop.Services.Tax;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Tax.Avalara.Components
{
    /// <summary>
    /// Represents a view component to render the button on a product list view
    /// </summary>
    [ViewComponent(Name = AvalaraTaxDefaults.ExportItemsViewComponentName)]
    public class ExportItemsViewComponent : NopViewComponent
    {
        #region Fields

        private readonly IPermissionService _permissionService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public ExportItemsViewComponent(IPermissionService permissionService,
            ITaxService taxService,
            IWorkContext workContext)
        {
            this._permissionService = permissionService;
            this._taxService = taxService;
            this._workContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke the widget view component
        /// </summary>
        /// <param name="widgetZone">Widget zone</param>
        /// <param name="additionalData">Additional parameters</param>
        /// <returns>View component result</returns>
        public IViewComponentResult Invoke(string widgetZone, object additionalData)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content(string.Empty);

            //ensure that Avalara tax provider is active
            if (!(_taxService.LoadActiveTaxProvider(_workContext.CurrentCustomer) is AvalaraTaxProvider))
                return Content(string.Empty);

            //ensure that it's a proper widget zone
            if (!widgetZone.Equals(AdminWidgetZones.ProductListButtons))
                return Content(string.Empty);

            return View("~/Plugins/Tax.Avalara/Views/Product/ExportItems.cshtml");
        }

        #endregion
    }
}