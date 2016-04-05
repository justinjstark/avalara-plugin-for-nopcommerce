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
using Nop.Services.Logging;
using Nop.Services.Tax;

namespace Nop.Plugin.Tax.Avalara
{
    /// <summary>
    /// Avalara tax provider
    /// </summary>
    public class AvalaraTaxProvider : BasePlugin, ITaxProvider
    {
        private const string TAXRATE_KEY = "Nop.taxrate.id-{0}";
        private const string AVATAX_CLIENT = "nopCommerce Avalara tax rate provider 1.0";

        private readonly AvalaraTaxSettings _avalaraTaxSettings;
        private readonly ICacheManager _cacheManager;
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;        

        public AvalaraTaxProvider(AvalaraTaxSettings avalaraTaxSettings,
            ICacheManager cacheManager,
            ILogger logger,
            ISettingService settingService)
        {
            this._avalaraTaxSettings = avalaraTaxSettings;
            this._cacheManager = cacheManager;
            this._logger = logger;
            this._settingService = settingService;            
        }
        
        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest)
        {
            if (calculateTaxRequest.Address == null)
                return new CalculateTaxResult { Errors = new List<string>() { "Address is not set" } };
            var cacheKey = string.Format(TAXRATE_KEY, calculateTaxRequest.Address.Id);
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
            var taxRequest = new AvalaraTaxRequest
            {
                CustomerCode = calculateTaxRequest.Customer != null ? calculateTaxRequest.Customer.CustomerGuid.ToString() : null,
                Addresses = new [] { address }
            };
            var result = GetTaxResult(taxRequest);
            if (result == null)
                return new CalculateTaxResult { Errors = new List<string>() { "Bad request" } };
            
            var errors = new List<string>();
            var tax = decimal.Zero;
            if (result.ResultCode.Equals(SeverityLevel.Success))
                tax = result.TotalTax;
            else
                foreach (var message in result.Messages)
                    errors.Add(message.Summary);

            _cacheManager.Set(cacheKey, result.TotalTax, 60);
            return new CalculateTaxResult { Errors = errors, TaxRate = tax};
        }

        public AvalaraTaxResult GetTaxResult(AvalaraTaxRequest taxRequest)
        {
            var line = new Line
            {
                LineNo = "1",
                DestinationCode = taxRequest.Addresses[0].AddressCode,
                OriginCode = taxRequest.Addresses[0].AddressCode,
                Qty = 1,
                Amount = 100
            };
            taxRequest.DocDate = DateTime.UtcNow.ToString("yyyy-MM-dd"); 
            taxRequest.CompanyCode = _avalaraTaxSettings.CompanyCode;
            taxRequest.Client = AVATAX_CLIENT;
            taxRequest.DetailLevel = DetailLevel.Tax;
            taxRequest.DocType = _avalaraTaxSettings.SaveRequests ? DocType.SalesInvoice : DocType.SalesOrder;
            taxRequest.Lines = new [] { line };
            var streamTaxRequest = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taxRequest));

            var serviceUrl = _avalaraTaxSettings.SandboxEnvironment ? "https://development.avalara.net" : "https://avatax.avalara.net";
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

            var avalaraTaxResult = new AvalaraTaxResult();
            try
            {
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var stringResult = streamReader.ReadToEnd();
                    avalaraTaxResult = JsonConvert.DeserializeObject<AvalaraTaxResult>(stringResult);
                }
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var stringResult = streamReader.ReadToEnd();
                    avalaraTaxResult = JsonConvert.DeserializeObject<AvalaraTaxResult>(stringResult);
                    _logger.Error(string.Format("Avalara errors: {0}", avalaraTaxResult.Messages.Aggregate(string.Empty,
                        (current, next) => string.Format("{0}, {1}", current, next.Summary)).Trim(',')), ex);
                }
            }

            return avalaraTaxResult;
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
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new AvalaraTaxSettings
            {
                AccountId = string.Empty,
                LicenseKey = string.Empty,
                CompanyCode = string.Empty,
                SaveRequests = false,
                SandboxEnvironment = false
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.AccountId", "Account ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.AccountId.Hint", "Avalara account ID.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode", "Company code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode.Hint", "Your company code in Avalara account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey", "License key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey.Hint", "Avalara account license key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.SandboxEnvironment", "Sandbox environment");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.SandboxEnvironment.Hint", "Use development account (for testing).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.SaveRequests", "Save requests");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.SaveRequests.Hint", "Save tax calculation requests in the tax history on your Avalara account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Testing", "Test tax calculation");

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<AvalaraTaxSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.AccountId");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.AccountId.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.SandboxEnvironment");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.SandboxEnvironment.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.SaveRequests");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.SaveRequests.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Testing");

            base.Uninstall();
        }
    }
}
