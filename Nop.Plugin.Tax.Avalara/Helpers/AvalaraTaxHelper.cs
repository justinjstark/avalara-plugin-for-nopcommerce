using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Nop.Plugin.Tax.Avalara.Domain;
using Nop.Services.Logging;

namespace Nop.Plugin.Tax.Avalara.Helpers
{
    /// <summary>
    /// Represents helper for the Avalara tax provider 
    /// </summary>
    public static class AvalaraTaxHelper
    {
        #region Properties

        /// <summary>
        /// Get an identifier of software client generating the API call
        /// </summary>
        public static string AVATAX_CLIENT
        {
            get { return "nopCommerce-AvalaraTaxRateProvider-1.32"; }
        }

        /// <summary>
        /// Get an identifier of certified integration
        /// </summary>
        public static string AVATAX_UID
        {
            get { return "a0o33000004BoPM"; }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get Avalara tax provider service URL
        /// </summary>
        /// <param name="isSandbox">Whether it is sandbox environment</param>
        /// <returns>URL</returns>
        private static string GetServiceUrl(bool isSandbox)
        {
            return isSandbox 
                ? "https://development.avalara.net/" 
                : "https://avatax.avalara.net/";
        }

        /// <summary>
        /// Check that plugin is configured
        /// </summary>
        /// <param name="avalaraTaxSettings">Avalara tax settings</param>
        /// <returns>True if is configured; otherwise false</returns>
        private static bool IsConfigured(AvalaraTaxSettings avalaraTaxSettings)
        {
            return !string.IsNullOrEmpty(avalaraTaxSettings.AccountId) && !string.IsNullOrEmpty(avalaraTaxSettings.LicenseKey);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Post tax request and get response from Avalara API
        /// </summary>
        /// <param name="taxRequest">Tax calculation request</param>
        /// <param name="avalaraTaxSettings">Avalara tax settings</param>
        /// <param name="logger">Logger</param>
        /// <returns>The response from Avalara API</returns>
        public static TaxResponse PostTaxRequest(TaxRequest taxRequest, AvalaraTaxSettings avalaraTaxSettings, ILogger logger)
        {
            if (!IsConfigured(avalaraTaxSettings))
            {
                logger.Error("Avalara tax provider is not configured");
                return null;
            }

            //validate addresses
            if (avalaraTaxSettings.ValidateAddresses)
            {
                var errors = new List<Message>();
                foreach (var address in taxRequest.Addresses)
                {
                    //(only for US or Canadian address)
                    if (address.Country == null || 
                        (!address.Country.Equals("us", StringComparison.InvariantCultureIgnoreCase) &&
                        !address.Country.Equals("ca", StringComparison.InvariantCultureIgnoreCase)))
                        continue;

                    var validatingResult = ValidateAddress(address, avalaraTaxSettings, logger);
                    if (validatingResult != null)
                    {
                        //set new addresses details
                        if (validatingResult.ResultCode == SeverityLevel.Success && validatingResult.ValidatedAddress != null)
                        {
                            address.City = validatingResult.ValidatedAddress.City;
                            address.Country = validatingResult.ValidatedAddress.Country;
                            address.Line1 = validatingResult.ValidatedAddress.Line1;
                            address.Line2 = validatingResult.ValidatedAddress.Line2;
                            address.PostalCode = validatingResult.ValidatedAddress.PostalCode;
                            address.Region = validatingResult.ValidatedAddress.Region;
                        }
                        else
                            errors.AddRange(validatingResult.Messages);
                    }
                    else
                        errors.Add(new Message() { Summary = "Avalara error on validating address " });
                }
                if (errors.Any())
                    return new TaxResponse() { Messages = errors.ToArray(), ResultCode = SeverityLevel.Error };
            }

            //create post data
            var postData = Encoding.Default.GetBytes(JsonConvert.SerializeObject(taxRequest));

            //create web request
            var url = string.Format("{0}1.0/tax/get", GetServiceUrl(avalaraTaxSettings.IsSandboxEnvironment));
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = postData.Length;

            //add authorization header
            var login = string.Format("{0}:{1}", avalaraTaxSettings.AccountId, avalaraTaxSettings.LicenseKey);
            var authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(login));
            request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", authorization));

            //add UID header
            request.Headers.Add("X-Avalara-UID", AVATAX_UID);

            try
            {
                //post request
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(postData, 0, postData.Length);
                }

                //get response
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<TaxResponse>(streamReader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    //log error
                    var responseText = streamReader.ReadToEnd();
                    logger.Error(string.Format("Avalara tax request error: {0}", responseText), ex);

                    return JsonConvert.DeserializeObject<TaxResponse>(responseText);
                }
            }
            catch (Exception ex)
            {
                //log error
                logger.Error("Avalara tax request error", ex);
                return null;
            }
        }

        /// <summary>
        /// Get tax rate (used for test connection)
        /// </summary>
        /// <param name="avalaraTaxSettings">Avalara tax settings</param>
        /// <returns>The response from Avalara API</returns>
        public static TaxResponse GetTaxRate(AvalaraTaxSettings avalaraTaxSettings)
        {
            if (!IsConfigured(avalaraTaxSettings))
                return null;

            //construct service url (coordinates for 100 Ravine Ln NE Suite 320, Bainbridge Island, WA 98110, US)
            var url = string.Format("{0}1.0/tax/{1},{2}/get", 
                GetServiceUrl(avalaraTaxSettings.IsSandboxEnvironment), "47.6253857", "-122.5185171");

            //add query parameters
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["saleamount"] = "100";
            uriBuilder.Query = query.ToString();

            //create web request
            var request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
            request.Method = "GET";
            request.ContentType = "application/json";

            //add authorization header
            var login = string.Format("{0}:{1}", avalaraTaxSettings.AccountId, avalaraTaxSettings.LicenseKey);
            var authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(login));
            request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", authorization));

            //add UID header
            request.Headers.Add("X-Avalara-UID", AVATAX_UID);

            try
            {
                //get response
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<TaxResponse>(streamReader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<TaxResponse>(streamReader.ReadToEnd());
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Post cancel tax request and get response from Avalara API
        /// </summary>
        /// <param name="cancelRequest">Cancel tax request</param>
        /// <param name="avalaraTaxSettings">Avalara tax settings</param>
        /// <param name="logger">Logger</param>
        /// <returns>The response from Avalara API</returns>
        public static CancelTaxResponse CancelTaxRequest(CancelTaxRequest cancelRequest, AvalaraTaxSettings avalaraTaxSettings, ILogger logger)
        {
            if (!IsConfigured(avalaraTaxSettings))
            {
                logger.Error("Avalara tax provider is not configured");
                return null;
            }

            //create post data
            var postData = Encoding.Default.GetBytes(JsonConvert.SerializeObject(cancelRequest));

            //create web request
            var url = string.Format("{0}1.0/tax/cancel", GetServiceUrl(avalaraTaxSettings.IsSandboxEnvironment));
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = postData.Length;

            //add authorization header
            var login = string.Format("{0}:{1}", avalaraTaxSettings.AccountId, avalaraTaxSettings.LicenseKey);
            var authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(login));
            request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", authorization));

            //add UID header
            request.Headers.Add("X-Avalara-UID", AVATAX_UID);

            try
            {
                //post request
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(postData, 0, postData.Length);
                }

                //get response
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<CancelTaxResponse>(streamReader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    //log error
                    var responseText = streamReader.ReadToEnd();
                    logger.Error(string.Format("Avalara cancel tax request error: {0}", responseText), ex);

                    return JsonConvert.DeserializeObject<CancelTaxResponse>(responseText);
                }
            }
            catch (Exception ex)
            {
                //log error
                logger.Error("Avalara cancel tax request error", ex);
                return null;
            }
        }

        /// <summary>
        /// Post validate address request and get response from Avalara API
        /// </summary>
        /// <param name="address">Address to validate</param>
        /// <param name="avalaraTaxSettings">Avalara tax settings</param>
        /// <param name="logger">Logger</param>
        /// <returns>The response from Avalara API</returns>
        public static ValidateAddressResponse ValidateAddress(Address address, AvalaraTaxSettings avalaraTaxSettings, ILogger logger)
        {
            if (!IsConfigured(avalaraTaxSettings))
            {
                logger.Error("Avalara tax provider is not configured");
                return null;
            }

            //construct service url
            var url = string.Format("{0}1.0/address/validate", GetServiceUrl(avalaraTaxSettings.IsSandboxEnvironment));

            //add query parameters
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["Line1"] = address.Line1;
            query["Line2"] = address.Line2;
            query["City"] = address.City;
            query["Region"] = address.Region;
            query["Country"] = address.Country;
            query["PostalCode"] = address.PostalCode;
            uriBuilder.Query = HttpUtility.UrlPathEncode(query.ToString());
            
            //create web request
            var request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
            request.Method = "GET";
            request.ContentType = "application/json";

            //add authorization header
            var login = string.Format("{0}:{1}", avalaraTaxSettings.AccountId, avalaraTaxSettings.LicenseKey);
            var authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(login));
            request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", authorization));

            //add UID header
            request.Headers.Add("X-Avalara-UID", AVATAX_UID);

            try
            {
                //get response
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<ValidateAddressResponse>(streamReader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    //log error
                    var responseText = streamReader.ReadToEnd();
                    logger.Error(string.Format("Avalara validate address error: {0}", responseText), ex);

                    return JsonConvert.DeserializeObject<ValidateAddressResponse>(responseText);
                }
            }
            catch (Exception ex)
            {
                //log error
                logger.Error("Avalara validate address error", ex);
                return null;
            }
        }

        #endregion
    }
}
