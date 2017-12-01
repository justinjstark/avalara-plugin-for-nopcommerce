using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Plugin.Tax.Avalara.Models.EntityUseCode;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Tax;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.Customers;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Tax.Avalara.Components
{
    /// <summary>
    /// Represents a view component to render an additional field on customer details, customer role details, product details, checkout attribute details views
    /// </summary>
    [ViewComponent(Name = AvalaraTaxDefaults.EntityUseCodeViewComponentName)]
    public class EntityUseCodeViewComponent : NopViewComponent
    {
        #region Fields

        private readonly AvalaraTaxManager _avalaraTaxManager;
        private readonly ICheckoutAttributeService _checkoutAttributeService;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IProductService _productService;
        private readonly IStaticCacheManager _cacheManager;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public EntityUseCodeViewComponent(AvalaraTaxManager avalaraTaxManager,
            ICheckoutAttributeService checkoutAttributeService,
            ICustomerService customerService,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            IProductService productService,
            IStaticCacheManager cacheManager,
            ITaxService taxService,
            IWorkContext workContext)
        {
            this._avalaraTaxManager = avalaraTaxManager;
            this._checkoutAttributeService = checkoutAttributeService;
            this._customerService = customerService;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._productService = productService;
            this._cacheManager = cacheManager;
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
            //ensure that model identifier is passed
            if (!(additionalData is int modelId) || modelId == 0)
                return Content(string.Empty);

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content(string.Empty);

            //ensure that Avalara tax provider is active
            if (!(_taxService.LoadActiveTaxProvider(_workContext.CurrentCustomer) is AvalaraTaxProvider))
                return Content(string.Empty);

            //ensure that it's a proper widget zone
            if (!widgetZone.Equals(AvalaraTaxDefaults.CustomerDetailsWidgetZone) &&
                !widgetZone.Equals(AvalaraTaxDefaults.CustomerRoleDetailsWidgetZone) &&
                !widgetZone.Equals(AvalaraTaxDefaults.ProductDetailsWidgetZone) &&
                !widgetZone.Equals(AvalaraTaxDefaults.CheckoutAttributeDetailsWidgetZone))
            {
                return Content(string.Empty);
            }

            //get Avalara pre-defined entity use codes
            var cachedEntityUseCodes = _cacheManager.Get(AvalaraTaxDefaults.EntityUseCodesCacheKey, () => _avalaraTaxManager.GetEntityUseCodes());
            var entityUseCodes = cachedEntityUseCodes?.Select(useCode => new SelectListItem
            {
                Value = useCode.code,
                Text = $"{useCode.name} ({useCode.validCountries.Aggregate(string.Empty, (list, country) => $"{list}{country},").TrimEnd(',')})"
            }).ToList() ?? new List<SelectListItem>();

            //add the special item for 'undefined' with empty guid value
            var defaultValue = Guid.Empty.ToString();
            entityUseCodes.Insert(0, new SelectListItem
            {
                Value = defaultValue,
                Text = _localizationService.GetResource("Plugins.Tax.Avalara.Fields.EntityUseCode.None")
            });

            //prepare model
            var model = new EntityUseCodeModel
            {
                Id = modelId,
                EntityUseCodes = entityUseCodes
            };

            //get entity by the model identifier
            BaseEntity entity = null;
            if (widgetZone.Equals(AvalaraTaxDefaults.CustomerDetailsWidgetZone))
            {
                var customerModel = new CustomerModel();
                model.PrecedingElementId = nameof(customerModel.IsTaxExempt);
                entity = _customerService.GetCustomerById(modelId);
            }

            if (widgetZone.Equals(AvalaraTaxDefaults.CustomerRoleDetailsWidgetZone))
            {
                var customerModel = new CustomerRoleModel();
                model.PrecedingElementId = nameof(customerModel.TaxExempt);
                entity = _customerService.GetCustomerRoleById(modelId);
            }

            if (widgetZone.Equals(AvalaraTaxDefaults.ProductDetailsWidgetZone))
            {
                var customerModel = new ProductModel();
                model.PrecedingElementId = nameof(customerModel.IsTaxExempt);
                entity = _productService.GetProductById(modelId);
            }

            if (widgetZone.Equals(AvalaraTaxDefaults.CheckoutAttributeDetailsWidgetZone))
            {
                var customerModel = new CheckoutAttributeModel();
                model.PrecedingElementId = nameof(customerModel.IsTaxExempt);
                entity = _checkoutAttributeService.GetCheckoutAttributeById(modelId);
            }

            //try to get previously saved entity use code
            model.AvalaraEntityUseCode = entity?.GetAttribute<string>(AvalaraTaxDefaults.EntityUseCodeAttribute) ?? defaultValue;

            return View("~/Plugins/Tax.Avalara/Views/EntityUseCode/EntityUseCode.cshtml", model);
        }

        #endregion
    }
}