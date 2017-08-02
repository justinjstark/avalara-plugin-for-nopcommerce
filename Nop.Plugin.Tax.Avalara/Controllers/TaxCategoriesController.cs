using System;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Controllers;
using Nop.Admin.Extensions;
using Nop.Admin.Models.Tax;
using Nop.Core;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Common;
using Nop.Services.Security;
using Nop.Services.Tax;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Tax.Avalara.Controllers
{
    public partial class TaxCategoriesController : BaseAdminController
	{
        #region Fields

        private readonly AvalaraImportManager _avalaraImportManager;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor

        public TaxCategoriesController(AvalaraImportManager avalaraImportManager,
            IGenericAttributeService genericAttributeService,
            ITaxCategoryService taxCategoryService,
            IPermissionService permissionService)
		{
            this._avalaraImportManager = avalaraImportManager;
            this._genericAttributeService = genericAttributeService;
            this._taxCategoryService = taxCategoryService;
            this._permissionService = permissionService;
		}

		#endregion 

        #region Methods

        public virtual ActionResult Categories()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            //use overridden view with displaying AvaTax system tax codes
            return View("~/Plugins/Tax.Avalara/Views/Categories.cshtml");
        }

        [HttpPost]
        public virtual ActionResult Categories(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedKendoGridJson();
            
            var taxCategories = new PagedList<TaxCategory>(_taxCategoryService.GetAllTaxCategories().AsQueryable(), command.Page - 1, command.PageSize);
            var categoriesModel = taxCategories.Select(taxCategory =>
            {
                var model = taxCategory.ToModel();

                //use tax code or the name as a system name
                var taxCode = taxCategory.GetAttribute<string>(_avalaraImportManager.AvaTaxCodeAttribute);
                model.CustomProperties.Add("SystemName", !string.IsNullOrEmpty(taxCode) ? taxCode : taxCategory.Name);

                return model;
            });
            
            return Json(new DataSourceResult
            {
                Data = categoriesModel,
                Total = taxCategories.TotalCount
            });
        }

        [HttpPost]
        public virtual ActionResult CategoryUpdate(TaxCategoryModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

            var taxCategory = _taxCategoryService.GetTaxCategoryById(model.Id);
            taxCategory = model.ToEntity(taxCategory);
            _taxCategoryService.UpdateTaxCategory(taxCategory);

            //try to get tax code from the model custom properties
            object systemName;
            if (model.CustomProperties.TryGetValue("SystemName", out systemName))
            {
                var taxCode = systemName is string[] ? ((string[])systemName).FirstOrDefault() : systemName is string ? (string)systemName : null;
                _genericAttributeService.SaveAttribute(taxCategory, _avalaraImportManager.AvaTaxCodeAttribute, taxCode);
            }

            return new NullJsonResult();
        }

        [HttpPost]
        public virtual ActionResult CategoryAdd([Bind(Exclude = "Id")] TaxCategoryModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

            var taxCategory = new TaxCategory();
            taxCategory = model.ToEntity(taxCategory);
            _taxCategoryService.InsertTaxCategory(taxCategory);

            //try to get tax code from the model custom properties
            object systemName;
            if (model.CustomProperties.TryGetValue("SystemName", out systemName))
            {
                var taxCode = systemName is string[] ? ((string[])systemName).FirstOrDefault() : systemName is string ? (string)systemName : null;
                _genericAttributeService.SaveAttribute(taxCategory, _avalaraImportManager.AvaTaxCodeAttribute, taxCode);
            }

            return new NullJsonResult();
        }

        [HttpPost]
        public virtual ActionResult CategoryDelete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            var taxCategory = _taxCategoryService.GetTaxCategoryById(id);
            if (taxCategory == null)
                throw new ArgumentException("No tax category found with the specified id");

            //delete tax code attribute
            _genericAttributeService.SaveAttribute<string>(taxCategory, _avalaraImportManager.AvaTaxCodeAttribute, null);

            _taxCategoryService.DeleteTaxCategory(taxCategory);

            return new NullJsonResult();
        }

        #endregion
    }
}
