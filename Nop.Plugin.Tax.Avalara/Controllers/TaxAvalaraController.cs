using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Tax.Avalara.Domain;
using Nop.Plugin.Tax.Avalara.Helpers;
using Nop.Plugin.Tax.Avalara.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Tax.Avalara.Controllers
{
    [AdminAuthorize]
    public class TaxAvalaraController : BasePluginController
    {
        #region Fields

        private readonly AvalaraTaxSettings _avalaraTaxSettings;
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public TaxAvalaraController(AvalaraTaxSettings avalaraTaxSettings,
            IAddressService addressService,
            ICountryService countryService,
            ILocalizationService localizationService,
            ILogger logger,
            ISettingService settingService,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            this._avalaraTaxSettings = avalaraTaxSettings;
            this._addressService = addressService;
            this._countryService = countryService;
            this._localizationService = localizationService;
            this._logger = logger;
            this._settingService = settingService;
            this._stateProvinceService = stateProvinceService;
            this._storeContext = storeContext;
            this._workContext = workContext; 
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Check that plugin is configured
        /// </summary>
        /// <returns>True if is configured; otherwise false</returns>
        protected bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_avalaraTaxSettings.AccountId) && !string.IsNullOrEmpty(_avalaraTaxSettings.LicenseKey);
        }

        /// <summary>
        /// Prepare address model
        /// </summary>
        /// <param name="model">Address model</param>
        protected void PrepareAddress(TaxAvalaraAddressModel model)
        {
            //populate list of countries
            model.AvailableCountries = _countryService.GetAllCountries(showHidden: true)
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToList();
            model.AvailableCountries.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });

            //populate list of states and provinces
            if (model.CountryId != 0)
                model.AvailableStates = _stateProvinceService.GetStateProvincesByCountryId(model.CountryId, showHidden: true)
                    .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToList();
            if (!model.AvailableStates.Any())
                model.AvailableStates.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Address.OtherNonUS"), Value = "0" });
        }

        #endregion

        #region Methods

        [ChildActionOnly]
        public ActionResult Configure()
        {
            //prepare model
            var model = new TaxAvalaraModel
            {
                IsConfigured = IsConfigured(),
                AccountId = _avalaraTaxSettings.AccountId,
                LicenseKey = _avalaraTaxSettings.LicenseKey,
                CompanyCode = _avalaraTaxSettings.CompanyCode,
                IsSandboxEnvironment = _avalaraTaxSettings.IsSandboxEnvironment,
                CommitTransactions = _avalaraTaxSettings.CommitTransactions,
                ValidateAddresses = _avalaraTaxSettings.ValidateAddresses
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

            //save settings
            _avalaraTaxSettings.AccountId = model.AccountId;
            _avalaraTaxSettings.LicenseKey = model.LicenseKey;
            _avalaraTaxSettings.CompanyCode = model.CompanyCode;
            _avalaraTaxSettings.IsSandboxEnvironment = model.IsSandboxEnvironment;
            _avalaraTaxSettings.CommitTransactions = model.CommitTransactions;
            _avalaraTaxSettings.ValidateAddresses = model.ValidateAddresses;
            _settingService.SaveSetting(_avalaraTaxSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            //prepare model
            model.IsConfigured = IsConfigured();
            PrepareAddress(model.TestAddress);

            return View("~/Plugins/Tax.Avalara/Views/TaxAvalara/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("testConnection")]
        public ActionResult TestConnection(TaxAvalaraModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //test connection
            var result = AvalaraTaxHelper.GetTaxRate(_avalaraTaxSettings);

            //display results
            if (result.Return(response => response.ResultCode, SeverityLevel.Error) == SeverityLevel.Success)
                SuccessNotification(_localizationService.GetResource("Plugins.Tax.Avalara.Credentials.Verified"));
            else
                ErrorNotification(_localizationService.GetResource("Plugins.Tax.Avalara.Credentials.Declined"));

            //prepare model
            model.IsConfigured = IsConfigured();
            PrepareAddress(model.TestAddress);

            return View("~/Plugins/Tax.Avalara/Views/TaxAvalara/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("testTax")]
        public ActionResult TestRequest(TaxAvalaraModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //create test tax request
            var taxRequest = new TaxRequest
            {
                Commit = true,
                Client = AvalaraTaxHelper.AVATAX_CLIENT,
                CompanyCode = _avalaraTaxSettings.CompanyCode,
                CustomerCode = _workContext.CurrentCustomer.Return(customer => customer.Id.ToString(), null),
                CurrencyCode = _workContext.WorkingCurrency.Return(currency => currency.CurrencyCode, null),
                DetailLevel = DetailLevel.Tax,
                DocCode = string.Format("Test-{0}", Guid.NewGuid()),
                DocType = _avalaraTaxSettings.CommitTransactions ? DocType.SalesInvoice : DocType.SalesOrder,
                DocDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };

            //set destination and origin addresses
            var addresses = new List<Address>();

            var destinationCountry = _countryService.GetCountryById(model.TestAddress.CountryId);
            var destinationStateOrProvince = _stateProvinceService.GetStateProvinceById(model.TestAddress.RegionId);
            addresses.Add(new Address
            {
                AddressCode = "0",
                Line1 = model.TestAddress.Address,
                City = model.TestAddress.City,
                Region = destinationStateOrProvince.Return(state => state.Abbreviation, null),
                Country = destinationCountry.Return(country => country.TwoLetterIsoCode, null),
                PostalCode = model.TestAddress.ZipPostalCode
            });

            //load settings for shipping origin address identifier
            var originAddressId = _settingService.GetSettingByKey<int>("ShippingSettings.ShippingOriginAddressId",
                storeId: _storeContext.CurrentStore.Id, loadSharedValueIfNotFound: true);

            //try get address that will be used for tax origin 
            var originAddress = _addressService.GetAddressById(originAddressId);
            if (originAddress != null)
            {
                addresses.Add(new Address
                {
                    AddressCode = originAddress.Id.ToString(),
                    Line1 = originAddress.Address1,
                    Line2 = originAddress.Address2,
                    City = originAddress.City,
                    Region = originAddress.StateProvince.Return(state => state.Abbreviation, null),
                    Country = originAddress.Country.Return(country => country.TwoLetterIsoCode, null),
                    PostalCode = originAddress.ZipPostalCode
                });
            }

            taxRequest.Addresses = addresses.ToArray();

            //create test item line
            var item = new Line
            {
                Amount = 100,
                Description = "Item line for the test tax request",
                DestinationCode = "0",
                ItemCode = "Test item line",
                LineNo = "test",
                OriginCode = originAddress.Return(address => address.Id, 0).ToString(),
                Qty = 1,
                TaxCode = "test"
            };
            taxRequest.Lines = new[] { item };

            //post tax request
            var taxResult = AvalaraTaxHelper.PostTaxRequest(taxRequest, _avalaraTaxSettings, _logger);

            //display results
            var resultstring = new StringBuilder();
            if (taxResult.Return(result => result.ResultCode, SeverityLevel.Error) == SeverityLevel.Success)
            {
                //display validated address (only for US or Canadian address)
                if (_avalaraTaxSettings.ValidateAddresses && taxResult.TaxAddresses != null && taxResult.TaxAddresses.Any() &&
                    (destinationCountry.TwoLetterIsoCode.Equals("us", StringComparison.InvariantCultureIgnoreCase) ||
                    destinationCountry.TwoLetterIsoCode.Equals("ca", StringComparison.InvariantCultureIgnoreCase)))
                {
                    resultstring.Append("Validated address <br />");
                    resultstring.AppendFormat("Postal / Zip code: {0}<br />", HttpUtility.HtmlEncode(taxResult.TaxAddresses[0].PostalCode));
                    resultstring.AppendFormat("Country: {0}<br />", HttpUtility.HtmlEncode(taxResult.TaxAddresses[0].Country));
                    resultstring.AppendFormat("Region: {0}<br />", HttpUtility.HtmlEncode(taxResult.TaxAddresses[0].Region));
                    resultstring.AppendFormat("City: {0}<br />", HttpUtility.HtmlEncode(taxResult.TaxAddresses[0].City));
                    resultstring.AppendFormat("Address: {0}<br /><br />", HttpUtility.HtmlEncode(taxResult.TaxAddresses[0].Address));
                }

                //display tax rates by jurisdictions
                if (taxResult.TaxLines != null && taxResult.TaxLines.Any())
                {
                    resultstring.AppendFormat("Total tax rate: {0:0.00}%<br />", taxResult.TaxLines[0].Rate * 100);
                    if (taxResult.TaxLines[0].TaxDetails != null)
                        foreach (var taxDetail in taxResult.TaxLines[0].TaxDetails)
                            resultstring.AppendFormat("Jurisdiction: {0}, Tax rate: {1:0.00}%<br />", HttpUtility.HtmlEncode(taxDetail.JurisName), taxDetail.Rate * 100);
                }
            }
            else
            {
                resultstring.Append("<font color=\"red\">");
                foreach (var message in taxResult.Messages)
                    resultstring.AppendFormat("{0}<br />", HttpUtility.HtmlEncode(message.Summary));
                resultstring.Append("</font>");
            }

            //prepare model
            model.TestTaxResult = resultstring.ToString();
            model.IsConfigured = IsConfigured();
            PrepareAddress(model.TestAddress);

            return View("~/Plugins/Tax.Avalara/Views/TaxAvalara/Configure.cshtml", model);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetStatesByCountryId(int countryId)
        {
            //get states and provinces for specified country
            var states = _stateProvinceService.GetStateProvincesByCountryId(countryId, showHidden: true)
                .Select(x => new { id = x.Id, name = x.Name }).ToList();
            if (!states.Any())
                states.Insert(0, new { id = 0, name = _localizationService.GetResource("Admin.Address.OtherNonUS") });

            return Json(states, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}