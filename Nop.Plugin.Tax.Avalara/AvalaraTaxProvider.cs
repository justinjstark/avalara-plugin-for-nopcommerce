using System;
using System.Collections.Generic;
using System.IO;
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
        private const string TAXRATE_KEY = "Nop.taxrate.id-{0}";
        private const string SERVICE_URL = "https://avatax.avalara.net/1.0/";
        private const string AVATAX_CLIENT = "nopCommerce Avalara tax rate provider 1.0";

        private readonly ISettingService _settingService;
        private readonly AvalaraTaxSettings _avalaraTaxSettings;

        public AvalaraTaxProvider(ISettingService settingService, AvalaraTaxSettings avalaraTaxSettings)
        {
            this._settingService = settingService;
            this._avalaraTaxSettings = avalaraTaxSettings;
        }
        
        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest)
        {
            if (calculateTaxRequest.Address == null)
                return new CalculateTaxResult() { Errors = new List<string>() { "Address is not set" } };
            string cacheKey = string.Format(TAXRATE_KEY, calculateTaxRequest.Address.Id);
            if (CacheManager.IsSet(cacheKey))
                return new CalculateTaxResult() { TaxRate = CacheManager.Get<decimal>(cacheKey) };

            var address = new Address()
            {
                AddressCode = calculateTaxRequest.Address.Id.ToString(),
                Line1 = calculateTaxRequest.Address.Address1,
                Line2 = calculateTaxRequest.Address.Address2,
                City = calculateTaxRequest.Address.City,
                Region = calculateTaxRequest.Address.StateProvince != null ? calculateTaxRequest.Address.StateProvince.Abbreviation : null,
                Country = calculateTaxRequest.Address.Country != null ? calculateTaxRequest.Address.Country.TwoLetterIsoCode : null,
                PostalCode = calculateTaxRequest.Address.ZipPostalCode
            };
            var taxRequest = new AvalaraTaxRequest()
            {
                CustomerCode = calculateTaxRequest.Customer != null ? calculateTaxRequest.Customer.Id.ToString() : null,
                Addresses = new Address[] { address }
            };
            var result = GetTaxResult(taxRequest);

            var errors = new List<string>();
            decimal tax = decimal.Zero;
            if (result.ResultCode.Equals(SeverityLevel.Success))
                tax = result.TotalTax;
            else
                foreach (Message message in result.Messages)
                    errors.Add(message.Summary);

            CacheManager.Set(cacheKey, result.TotalTax, 60);
            return new CalculateTaxResult() { Errors = errors, TaxRate = tax};
        }

        public AvalaraTaxResult GetTaxResult(AvalaraTaxRequest taxRequest)
        {
            var line = new Line()
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
            taxRequest.DocType = DocType.SalesInvoice;
            taxRequest.Lines = new Line[] { line };
            var streamTaxRequest = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taxRequest));

            var login = string.Format("{0}:{1}", _avalaraTaxSettings.AccountId, _avalaraTaxSettings.LicenseKey);
            var authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(login));
            var request = (HttpWebRequest)WebRequest.Create(string.Format("{0}tax/get", SERVICE_URL));
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
                }
            }

            return avalaraTaxResult;
        }

        /// <summary>
        /// Gets a cache manager
        /// </summary>
        public ICacheManager CacheManager
        {
            get
            {
                return new MemoryCacheManager();
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
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new AvalaraTaxSettings()
            {
                AccountId = string.Empty,
                LicenseKey = string.Empty,
                CompanyCode = string.Empty
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.AccountId", "Account ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.AccountId.Hint", "Avalara account ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey", "License key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey.Hint", "Avalara account license key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode", "Company code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode.Hint", "Your company code in Avalara account");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Country", "Country");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Region", "State/Province");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.City", "City");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Address", "Address");
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
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.LicenseKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.CompanyCode.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Country");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Region");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.City");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Address");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Testing");

            base.Uninstall();
        }
    }
}
