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
        private const string PATH_VIEW = "~/Plugins/Tax.Avalara/Views/TaxAvalara/Configure.cshtml";

        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly AvalaraTaxSettings _avalaraTaxSettings;
        private readonly ISettingService _settingService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ILocalizationService _localizationService;

        public TaxAvalaraController(ITaxService taxService,
            IWorkContext workContext,
            AvalaraTaxSettings avalaraTaxSettings,
            ISettingService settingService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            ILocalizationService localizationService)
        {
            this._taxService = taxService;
            this._workContext = workContext;
            this._avalaraTaxSettings = avalaraTaxSettings;
            this._settingService = settingService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._localizationService = localizationService;
        }

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

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new TaxAvalaraModel
            {
                AccountId = _avalaraTaxSettings.AccountId,
                LicenseKey = _avalaraTaxSettings.LicenseKey,
                CompanyCode = _avalaraTaxSettings.CompanyCode,
                SaveRequests = _avalaraTaxSettings.SaveRequests,
                SandboxEnvironment = _avalaraTaxSettings.SandboxEnvironment
            };

            PrepareAddress(model.TestAddress);

            return View(PATH_VIEW, model);
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
            _avalaraTaxSettings.SaveRequests = model.SaveRequests;
            _avalaraTaxSettings.SandboxEnvironment = model.SandboxEnvironment;
            _settingService.SaveSetting(_avalaraTaxSettings);
            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            PrepareAddress(model.TestAddress);

            return View(PATH_VIEW, model);
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
            var taxRequest = new AvalaraTaxRequest
            {
                CustomerCode = _workContext.CurrentCustomer.CustomerGuid.ToString(),
                Addresses = new [] { address }
            };
            var taxProvider = (AvalaraTaxProvider)_taxService.LoadTaxProviderBySystemName("Tax.Avalara");

            var resultstring = new StringBuilder();
            var taxResult = taxProvider.GetTaxResult(taxRequest);
            if (taxResult.ResultCode.Equals(SeverityLevel.Success))
            {
                resultstring.AppendFormat("Total Tax: {0}%<br />", taxResult.TotalTax);
                if (taxResult.TaxLines != null)
                    foreach (var taxLine in taxResult.TaxLines)
                    {
                        resultstring.AppendFormat("&nbsp;{0}. Line Tax: {1}%<br />", HttpUtility.UrlEncode(taxLine.LineNo), taxLine.Tax);
                        if (taxLine.TaxDetails != null)
                            foreach (var taxDetail in taxLine.TaxDetails)
                                resultstring.AppendFormat("&nbsp;&nbsp;Jurisdiction: {0}, Tax: {1}%<br />", HttpUtility.UrlEncode(taxDetail.JurisName), taxDetail.Tax);
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

            return View(PATH_VIEW, model);
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
    }
}