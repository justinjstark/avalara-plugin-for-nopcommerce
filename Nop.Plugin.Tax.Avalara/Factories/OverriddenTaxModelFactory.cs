using System;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Common;
using Nop.Services.Tax;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Tax;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Tax.Avalara.Factories
{
    /// <summary>
    /// Represents overridden tax model factory
    /// </summary>
    public partial class OverriddenTaxModelFactory : TaxModelFactory
    {
        #region Fields

        private readonly AvalaraTaxManager _avalaraTaxManager;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IStaticCacheManager _cacheManager;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public OverriddenTaxModelFactory(AvalaraTaxManager avalaraTaxManager,
            IGenericAttributeService genericAttributeService,
            IStaticCacheManager cacheManager,
            ITaxCategoryService taxCategoryService,
            ITaxService taxService,
            IWorkContext workContext,
            TaxSettings taxSettings) : base(taxCategoryService,
                taxService,
                taxSettings)
        {
            this._avalaraTaxManager = avalaraTaxManager;
            this._genericAttributeService = genericAttributeService;
            this._cacheManager = cacheManager;
            this._taxCategoryService = taxCategoryService;
            this._taxService = taxService;
            this._workContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare paged tax category list model
        /// </summary>
        /// <param name="searchModel">Tax category search model</param>
        /// <returns>Tax category list model</returns>
        public override TaxCategoryListModel PrepareTaxCategoryListModel(TaxCategorySearchModel searchModel)
        {
            //ensure that Avalara tax provider is active
            if (!(_taxService.LoadActiveTaxProvider(_workContext.CurrentCustomer) is AvalaraTaxProvider))
                return base.PrepareTaxCategoryListModel(searchModel);

            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get tax categories
            var taxCategories = _taxCategoryService.GetAllTaxCategories();

            //get tax types and define the default value
            var taxTypes = _cacheManager.Get(AvalaraTaxDefaults.TaxCodeTypesCacheKey, () => _avalaraTaxManager.GetTaxCodeTypes())
                ?.Select(taxType => new { Id = taxType.Key, Name = taxType.Value });
            var defaultType = taxTypes
                ?.FirstOrDefault(taxType => taxType.Name.Equals("Unknown", StringComparison.InvariantCultureIgnoreCase))
                ?? taxTypes?.FirstOrDefault();

            //prepare grid model
            var model = new TaxCategoryListModel
            {
                Data = taxCategories.PaginationByRequestModel(searchModel).Select(taxCategory =>
                {
                    //fill in model values from the entity
                    var taxCategoryModel = new Models.Tax.TaxCategoryModel
                    {
                        Id = taxCategory.Id,
                        Name = taxCategory.Name,
                        DisplayOrder = taxCategory.DisplayOrder
                    };

                    //try to get previously saved tax code type and description
                    var taxCodeType = taxTypes?.FirstOrDefault(type =>
                        type.Id.Equals(_genericAttributeService.GetAttribute<string>(taxCategory, AvalaraTaxDefaults.TaxCodeTypeAttribute) ?? string.Empty))
                        ?? defaultType;
                    taxCategoryModel.Type = taxCodeType?.Name ?? string.Empty;
                    taxCategoryModel.TypeId = taxCodeType?.Id ?? Guid.Empty.ToString();
                    taxCategoryModel.Description = _genericAttributeService
                        .GetAttribute<string>(taxCategory, AvalaraTaxDefaults.TaxCodeDescriptionAttribute) ?? string.Empty;

                    return taxCategoryModel;
                }),
                Total = taxCategories.Count
            };

            return model;
        }

        #endregion
    }
}