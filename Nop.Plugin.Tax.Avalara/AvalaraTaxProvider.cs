using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Routing;
using Newtonsoft.Json;
using Nop.Core.Caching;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Tax;

namespace Nop.Plugin.Tax.Avalara
{
    /// <summary>
    /// Avalara tax provider
    /// </summary>
    public class AvalaraTaxProvider : BasePlugin, ITaxProvider
    {
        /// <summary>
        /// {0} - Address
        /// {1} - City
        /// {2} - StateProvinceId
        /// {3} - CountryId
        /// {4} - ZipPostalCode
        /// </summary>
        private const string TAXRATE_KEY = "Nop.taxrate.id-{0}-{1}-{2}-{3}-{4}";
        private const string AVATAX_CLIENT = "nopCommerce Avalara tax rate provider 1.2";

        #region Fields

        private readonly AvalaraTaxSettings _avalaraTaxSettings;
        private readonly ICacheManager _cacheManager;
        private readonly ISettingService _settingService;
        private readonly ITaxCategoryService _taxCategoryService;

        #endregion

        #region Ctor

        public AvalaraTaxProvider(AvalaraTaxSettings avalaraTaxSettings,
            ICacheManager cacheManager,
            ISettingService settingService,
            ITaxCategoryService taxCategoryService)
        {
            this._avalaraTaxSettings = avalaraTaxSettings;
            this._cacheManager = cacheManager;
            this._settingService = settingService;
            this._taxCategoryService = taxCategoryService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest)
        {
            if (calculateTaxRequest.Address == null)
                return new CalculateTaxResult { Errors = new List<string>() { "Address is not set" } };

            string cacheKey = string.Format(TAXRATE_KEY,
                !string.IsNullOrEmpty(calculateTaxRequest.Address.Address1) ? calculateTaxRequest.Address.Address1 : string.Empty,
                !string.IsNullOrEmpty(calculateTaxRequest.Address.City) ? calculateTaxRequest.Address.City : string.Empty,
                calculateTaxRequest.Address.StateProvince != null ? calculateTaxRequest.Address.StateProvince.Id : 0,
                calculateTaxRequest.Address.Country != null ? calculateTaxRequest.Address.Country.Id : 0,
                !string.IsNullOrEmpty(calculateTaxRequest.Address.ZipPostalCode) ? calculateTaxRequest.Address.ZipPostalCode : string.Empty);

            // we don't use standard way _cacheManager.Get() due the need write errors to CalculateTaxResult
            if (_cacheManager.IsSet(cacheKey))
                return new CalculateTaxResult { TaxRate = _cacheManager.Get<decimal>(cacheKey) };

            var address = new Address
            {
                AddressCode = calculateTaxRequest.Address.Id.ToString(),
                Line1 = calculateTaxRequest.Address.Address1,
                Line2 = calculateTaxRequest.Address.Address2,
                City = calculateTaxRequest.Address.City,
                Region = calculateTaxRequest.Address.StateProvince != null ? calculateTaxRequest.Address.StateProvince.Abbreviation : null,
                Country = calculateTaxRequest.Address.Country != null ? calculateTaxRequest.Address.Country.TwoLetterIsoCode : null,
                PostalCode = calculateTaxRequest.Address.ZipPostalCode
            };

            //information about product
            var taxCategory = _taxCategoryService.GetTaxCategoryById(calculateTaxRequest.TaxCategoryId);
            var line = new Line
            {
                LineNo = "1",
                DestinationCode = calculateTaxRequest.Address.Id.ToString(),
                OriginCode = calculateTaxRequest.Address.Id.ToString(),
                ItemCode = calculateTaxRequest.Product != null ? calculateTaxRequest.Product.Sku : null,
                Description = calculateTaxRequest.Product != null ? calculateTaxRequest.Product.ShortDescription : null,
                TaxCode = taxCategory != null ? taxCategory.Name : null,
                Qty = 1,
                Amount = calculateTaxRequest.Price
            };

            //tax exemption
            var exemptionReason = string.Empty;
            if (calculateTaxRequest.Product != null && calculateTaxRequest.Product.IsTaxExempt)
                exemptionReason = string.Format("Exempt-product-{0}", calculateTaxRequest.Product.Sku);
            else 
                if (calculateTaxRequest.Customer != null)
                    if (calculateTaxRequest.Customer.IsTaxExempt)
                        exemptionReason = string.Format("Exempt-customer-{0}", calculateTaxRequest.Customer.CustomerGuid.ToString());
                    else
                    {
                        var exemptRole = calculateTaxRequest.Customer.CustomerRoles.FirstOrDefault(role => role.Active && role.TaxExempt);
                        exemptionReason = exemptRole != null ? string.Format("Exempt-role-{0}", exemptRole.Name) : string.Empty;
                    }

            var taxRequest = new AvalaraTaxRequest
            {
                CustomerCode = calculateTaxRequest.Customer != null ? calculateTaxRequest.Customer.CustomerGuid.ToString() : null,
                ExemptionNo = !string.IsNullOrEmpty(exemptionReason) ? exemptionReason : null,
                Addresses = new [] { address },
                Lines = new [] { line }
            };

            var result = GetTaxResult(taxRequest);
            if (result == null)
                return new CalculateTaxResult { Errors = new List<string>() { "Bad request" } };
            
            if (result.ResultCode != SeverityLevel.Success)
                return new CalculateTaxResult { Errors = result.Messages.Select(message => message.Summary).ToList() };

            var taxRate = result.TaxLines != null && result.TaxLines.Any() ? result.TaxLines[0].Rate * 100 : 0;
            _cacheManager.Set(cacheKey, taxRate, 60);
            return new CalculateTaxResult { TaxRate = taxRate };
        }

        /// <summary>
        /// Get a response from Avalara API
        /// </summary>
        /// <param name="taxRequest">Tax calculation request</param>
        /// <returns>The response from Avalara API</returns>
        public AvalaraTaxResult GetTaxResult(AvalaraTaxRequest taxRequest)
        {
            taxRequest.Commit = true;
            taxRequest.DocCode = Guid.NewGuid().ToString();
            taxRequest.DocDate = DateTime.UtcNow.ToString("yyyy-MM-dd"); 
            taxRequest.CompanyCode = _avalaraTaxSettings.CompanyCode;
            taxRequest.Client = AVATAX_CLIENT;
            taxRequest.DetailLevel = DetailLevel.Tax;
            taxRequest.DocType = _avalaraTaxSettings.CommitTransactions ? DocType.SalesInvoice : DocType.SalesOrder;
            
            var streamTaxRequest = Encoding.Default.GetBytes(JsonConvert.SerializeObject(taxRequest));

            var serviceUrl = _avalaraTaxSettings.IsSandboxEnvironment ? "https://development.avalara.net" : "https://avatax.avalara.net";
            var login = string.Format("{0}:{1}", _avalaraTaxSettings.AccountId, _avalaraTaxSettings.LicenseKey);
            var authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(login));
            var request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/1.0/tax/get", serviceUrl));
            request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", authorization));
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = streamTaxRequest.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(streamTaxRequest, 0, streamTaxRequest.Length);
            }

            try
            {
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<AvalaraTaxResult>(streamReader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<AvalaraTaxResult>(streamReader.ReadToEnd());
                }
            }
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "TaxAvalara";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Tax.Avalara.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new AvalaraTaxSettings
            {
                CompanyCode = "APITrialCompany",
                IsSandboxEnvironment = true,
                CommitTransactions = true
            });

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.AccountId", "Account ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.AccountId.Hint", "Cpecify Avalara account ID.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.CommitTransactions", "Commit transactions");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.CommitTransactions.Hint", "Check for recording transactions in the history on your Avalara account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode", "Company code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode.Hint", "Enter your company code in Avalara account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey", "License key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey.Hint", "Cpecify Avalara account license key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.IsSandboxEnvironment", "Sandbox environment");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.IsSandboxEnvironment.Hint", "Check for using sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Testing", "Test tax calculation");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<AvalaraTaxSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.AccountId");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.AccountId.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.CommitTransactions");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.CommitTransactions.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.IsSandboxEnvironment");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.IsSandboxEnvironment.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Testing");

            base.Uninstall();
        }

        #endregion
    }
}
