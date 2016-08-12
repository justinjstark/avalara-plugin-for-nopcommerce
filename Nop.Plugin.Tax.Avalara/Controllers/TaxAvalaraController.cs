using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Tax.Avalara.Models;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Tax;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Tax.Avalara.Controllers
{
    [AdminAuthorize]
    public class TaxAvalaraController : BasePluginController
    {
        #region Fields

        private readonly AvalaraTaxSettings _avalaraTaxSettings;
        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public TaxAvalaraController(AvalaraTaxSettings avalaraTaxSettings,
            ICountryService countryService,
            ILocalizationService localizationService,
            ISettingService settingService,
            IStateProvinceService stateProvinceService,
            ITaxService taxService,
            IWorkContext workContext)
        {
            this._avalaraTaxSettings = avalaraTaxSettings;
            this._countryService = countryService;
            this._localizationService = localizationService;
            this._settingService = settingService;
            this._stateProvinceService = stateProvinceService;
            this._taxService = taxService;
            this._workContext = workContext; 
        }

        #endregion

        #region Utilities

        [NonAction]
        protected void PrepareAddress(TaxAvalaraAddressModel model)
        {
            model.AvailableCountries = _countryService.GetAllCountries(showHidden: true)
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToList();
            model.AvailableCountries.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });
            if (model.CountryId != 0)
                model.AvailableStates = _stateProvinceService.GetStateProvincesByCountryId(model.CountryId, showHidden: true)
                    .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToList();
            model.AvailableStates.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Address.OtherNonUS"), Value = "0" });
        }

        #endregion

        #region Methods

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new TaxAvalaraModel
            {
                AccountId = _avalaraTaxSettings.AccountId,
                LicenseKey = _avalaraTaxSettings.LicenseKey,
                CompanyCode = _avalaraTaxSettings.CompanyCode,
                IsSandboxEnvironment = _avalaraTaxSettings.IsSandboxEnvironment,
                CommitTransactions = _avalaraTaxSettings.CommitTransactions
            };
            PrepareAddress(model.TestAddress);

            return View("~/Plugins/Tax.Avalara/Views/TaxAvalara/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        public ActionResult Configure(TaxAvalaraModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            _avalaraTaxSettings.AccountId = model.AccountId;
            _avalaraTaxSettings.LicenseKey = model.LicenseKey;
            _avalaraTaxSettings.CompanyCode = model.CompanyCode;
            _avalaraTaxSettings.IsSandboxEnvironment = model.IsSandboxEnvironment;
            _avalaraTaxSettings.CommitTransactions = model.CommitTransactions;
            _settingService.SaveSetting(_avalaraTaxSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("test")]
        public ActionResult TestRequest(TaxAvalaraModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            var country = _countryService.GetCountryById(model.TestAddress.CountryId);
            var region = _stateProvinceService.GetStateProvinceById(model.TestAddress.RegionId);
            var address = new Address
            {
                AddressCode = "1",
                Country = country != null ? country.TwoLetterIsoCode : null,
                Region = region != null ? region.Abbreviation : null,
                City = model.TestAddress.City,
                Line1 = model.TestAddress.Address
            };

            var line = new Line
            {
                LineNo = "1",
                DestinationCode = "1",
                OriginCode = "1",
                Qty = 1
            };

            var taxRequest = new AvalaraTaxRequest
            {
                CustomerCode = _workContext.CurrentCustomer.CustomerGuid.ToString(),
                Addresses = new [] { address },
                Lines = new[] { line }
            };

            var resultstring = new StringBuilder();
            var taxProvider = (AvalaraTaxProvider)_taxService.LoadTaxProviderBySystemName("Tax.Avalara");
            var taxResult = taxProvider.GetTaxResult(taxRequest);
            if (taxResult.ResultCode == SeverityLevel.Success)
            {
                if (taxResult.TaxLines != null && taxResult.TaxLines.Any())
                {
                    resultstring.AppendFormat("Total tax rate: {0:0.00}%<br />", taxResult.TaxLines[0].Rate * 100);
                    if (taxResult.TaxLines[0].TaxDetails != null)
                        foreach (var taxDetail in taxResult.TaxLines[0].TaxDetails)
                            resultstring.AppendFormat("Jurisdiction: {0}, Tax rate: {1:0.00}%<br />", HttpUtility.UrlEncode(taxDetail.JurisName), taxDetail.Rate * 100);
                }
            }
            else
            {
                resultstring.Append("<font color=\"red\">");
                foreach (var message in taxResult.Messages)
                    resultstring.AppendFormat("{0}<br />", HttpUtility.HtmlEncode(message.Summary));
                resultstring.Append("</font>");
            }

            model.TestingResult = resultstring.ToString();
            PrepareAddress(model.TestAddress);

            return View("~/Plugins/Tax.Avalara/Views/TaxAvalara/Configure.cshtml", model);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetStatesByCountryId(int countryId)
        {
            var states = _stateProvinceService.GetStateProvincesByCountryId(countryId, showHidden: true)
                .Select(x => new { id = x.Id, name = x.Name }).ToList();
            if (states.Count == 0)
                states.Insert(0, new { id = 0, name = _localizationService.GetResource("Admin.Address.OtherNonUS") });

            return Json(states, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}