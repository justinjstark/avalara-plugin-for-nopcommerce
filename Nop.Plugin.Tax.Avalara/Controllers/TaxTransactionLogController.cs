using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Html;
using Nop.Plugin.Tax.Avalara.Domain;
using Nop.Plugin.Tax.Avalara.Models.Log;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Plugin.Tax.Avalara.Controllers
{
    public partial class TaxTransactionLogController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly ITaxTransactionLogService _taxTransactionLogService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public TaxTransactionLogController(ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            ITaxTransactionLogService taxTransactionLogService,
            IWorkContext workContext)
        {
            this._customerService = customerService;
            this._dateTimeHelper = dateTimeHelper;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._taxTransactionLogService = taxTransactionLogService;
            this._workContext = workContext;
        }

        #endregion

        #region Methods

        [HttpPost]
        public virtual IActionResult LogList(TaxTransactionLogListModel model, DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedKendoGridJson();

            //prepare filter parameters
            var createdFromValue = model.CreatedFrom.HasValue
                ? (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.CreatedFrom.Value, _dateTimeHelper.CurrentTimeZone) : null;
            var createdToValue = model.CreatedTo.HasValue
                ? (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.CreatedTo.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1) : null;
            var logType = model.LogTypeId > 0 ? (LogType?)(model.LogTypeId) : null;

            //get tax transaction log
            var taxtransactionLog = _taxTransactionLogService.GetTaxTransactionLog(logType: logType, 
                createdFromUtc: createdFromValue, createdToUtc: createdToValue,
                pageIndex: command.Page - 1, pageSize: command.PageSize);

            //prepare model
            var gridModel = new DataSourceResult
            {
                Data = taxtransactionLog.Select(logItem => new TaxTransactionLogModel
                {
                    Id = logItem.Id,
                    LogType = logItem.LogType.GetLocalizedEnum(_localizationService, _workContext),
                    Message = CommonHelper.EnsureMaximumLength(logItem.Message, 100, "..."),
                    CustomerId = logItem.CustomerId,
                    CreatedDate = _dateTimeHelper.ConvertToUserTime(logItem.CreatedDateUtc, DateTimeKind.Utc)
                }),
                Total = taxtransactionLog.TotalCount
            };

            return Json(gridModel);
        }
        
        public virtual IActionResult ClearAll()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            _taxTransactionLogService.ClearTaxTransactionLog();

            return Json(new { result = true });
        }

        public virtual IActionResult View(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            //try to get log item with the passed identifier
            var logItem = _taxTransactionLogService.GetTaxTransactionLogById(id);
            if (logItem == null)
                return RedirectToAction("Configure", "AvalaraTax");

            var model = new TaxTransactionLogModel
            {
                Id = logItem.Id,
                LogType = logItem.LogType.GetLocalizedEnum(_localizationService, _workContext),
                Message = HtmlHelper.FormatText(logItem.Message, false, true, false, false, false, false),
                CustomerId = logItem.CustomerId,
                CustomerEmail = _customerService.GetCustomerById(logItem.CustomerId)?.Email,
                CreatedDate = _dateTimeHelper.ConvertToUserTime(logItem.CreatedDateUtc, DateTimeKind.Utc)
            };

            return View("~/Plugins/Tax.Avalara/Views/Log/View.cshtml", model);
        }

        [HttpPost]
        public virtual IActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            //try to get log item with the passed identifier
            var logItem = _taxTransactionLogService.GetTaxTransactionLogById(id);
            if (logItem != null)
            {
                _taxTransactionLogService.DeleteTaxTransactionLog(logItem);
                SuccessNotification(_localizationService.GetResource("Plugins.Tax.Avalara.Log.Deleted"));
            }

            return RedirectToAction("Configure", "AvalaraTax");
        }

        #endregion
    }
}