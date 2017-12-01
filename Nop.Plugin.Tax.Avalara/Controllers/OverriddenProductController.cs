using System;
using System.Collections.Generic;
using System.Linq;
using Avalara.AvaTax.RestClient;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.ExportImport;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Controllers;

namespace Nop.Plugin.Tax.Avalara.Controllers
{
    public class OverriddenProductController : ProductController
    {
        #region Fields

        private readonly AvalaraTaxManager _avalaraTaxManager;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public OverriddenProductController(AvalaraTaxManager avalaraTaxManager,
            ITaxService taxService,
            IProductService productService,
            IProductTemplateService productTemplateService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ICustomerService customerService,
            IUrlRecordService urlRecordService,
            IWorkContext workContext,
            ILanguageService languageService,
            ILocalizationService localizationService,
            ILocalizedEntityService localizedEntityService,
            ISpecificationAttributeService specificationAttributeService,
            IPictureService pictureService,
            ITaxCategoryService taxCategoryService,
            IProductTagService productTagService,
            ICopyProductService copyProductService,
            IPdfService pdfService,
            IExportManager exportManager,
            IImportManager importManager,
            ICustomerActivityService customerActivityService,
            IPermissionService permissionService,
            IAclService aclService,
            IStoreService storeService,
            IOrderService orderService,
            IStoreMappingService storeMappingService,
            IVendorService vendorService,
            IDateRangeService dateRangeService,
            IShippingService shippingService,
            IShipmentService shipmentService,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            IMeasureService measureService,
            MeasureSettings measureSettings,
            IStaticCacheManager cacheManager,
            IDateTimeHelper dateTimeHelper,
            IDiscountService discountService,
            IProductAttributeService productAttributeService,
            IBackInStockSubscriptionService backInStockSubscriptionService,
            IShoppingCartService shoppingCartService,
            IProductAttributeFormatter productAttributeFormatter,
            IProductAttributeParser productAttributeParser,
            IDownloadService downloadService,
            ISettingService settingService,
            TaxSettings taxSettings,
            VendorSettings vendorSettings) : base(productService,
                productTemplateService,
                categoryService,
                manufacturerService,
                customerService,
                urlRecordService,
                workContext,
                languageService,
                localizationService,
                localizedEntityService,
                specificationAttributeService,
                pictureService,
                taxCategoryService,
                productTagService,
                copyProductService,
                pdfService,
                exportManager,
                importManager,
                customerActivityService,
                permissionService,
                aclService,
                storeService,
                orderService,
                storeMappingService,
                vendorService,
                dateRangeService,
                shippingService,
                shipmentService,
                currencyService,
                currencySettings,
                measureService,
                measureSettings,
                cacheManager,
                dateTimeHelper,
                discountService,
                productAttributeService,
                backInStockSubscriptionService,
                shoppingCartService,
                productAttributeFormatter,
                productAttributeParser,
                downloadService,
                settingService,
                taxSettings,
                vendorSettings)
        {
            this._avalaraTaxManager = avalaraTaxManager;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._productAttributeService = productAttributeService;
            this._productService = productService;
            this._taxCategoryService = taxCategoryService;
            this._taxService = taxService;
            this._workContext = workContext;
        }

        #endregion

        #region Methods

        [HttpPost]
        public IActionResult ExportProducts(string selectedIds)
        {
            //ensure that Avalara tax provider is active
            if (!(_taxService.LoadActiveTaxProvider(_workContext.CurrentCustomer) is AvalaraTaxProvider))
                return RedirectToAction("List", "Product");

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            //prepare exported items
            var productIds = selectedIds?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(id => Convert.ToInt32(id)).ToArray();
            var exportedItems = new List<ItemModel>();
            foreach (var product in _productService.GetProductsByIds(productIds))
            {
                //find product combinations
                var combinations = _productAttributeService.GetAllProductAttributeCombinations(product.Id)
                    .Where(combination => !string.IsNullOrEmpty(combination.Sku));

                //export items with specified SKU only
                if (string.IsNullOrEmpty(product.Sku) && !combinations.Any())
                    continue;

                //prepare common properties
                var taxCategory = _taxCategoryService.GetTaxCategoryById(product.TaxCategoryId);
                var taxCode = CommonHelper.EnsureMaximumLength(taxCategory?.Name, 25);
                var description = CommonHelper.EnsureMaximumLength(product.ShortDescription ?? product.Name, 255);

                //add the product as exported item
                if (!string.IsNullOrEmpty(product.Sku))
                {
                    exportedItems.Add(new ItemModel
                    {
                        createdDate = DateTime.UtcNow,
                        description = description,
                        itemCode = CommonHelper.EnsureMaximumLength(product.Sku, 50),
                        taxCode = taxCode
                    });
                }

                //add product combinations
                exportedItems.AddRange(combinations.Select(combination => new ItemModel
                {
                    createdDate = DateTime.UtcNow,
                    description = description,
                    itemCode = CommonHelper.EnsureMaximumLength(combination.Sku, 50),
                    taxCode = taxCode
                }));
            }

            //get existing items
            var existingItemCodes = _avalaraTaxManager.GetItems()?.Select(item => item.itemCode).ToList() ?? new List<string>();

            //remove duplicates
            exportedItems = exportedItems.Where(item => !existingItemCodes.Contains(item.itemCode)).Distinct().ToList();

            //export items
            if (exportedItems.Any())
            {
                //create items and get the result
                var result = _avalaraTaxManager.CreateItems(exportedItems)?.Count;

                //display results
                if (result.HasValue && result > 0)
                    SuccessNotification(string.Format(_localizationService.GetResource("Plugins.Tax.Avalara.Items.Export.Success"), result));
                else
                    ErrorNotification(_localizationService.GetResource("PPlugins.Tax.Avalara.Items.Export.Error"));
            }
            else
                SuccessNotification(_localizationService.GetResource("Plugins.Tax.Avalara.Items.Export.AlreadyExported"));

            return RedirectToAction("List", "Product");
        }

        #endregion
    }
}