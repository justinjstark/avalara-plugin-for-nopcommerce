using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Plugins;
using Nop.Plugin.Tax.Avalara.Domain;
using Nop.Plugin.Tax.Avalara.Helpers;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Tax;
using common = Nop.Core.Domain.Common;

namespace Nop.Plugin.Tax.Avalara
{
    /// <summary>
    /// Represents Avalara tax provider
    /// </summary>
    public class AvalaraTaxProvider : BasePlugin, ITaxProvider
    {
        #region Constants

        /// <summary>
        /// Key for caching tax rate for certain address
        /// </summary>
        /// <remarks>
        /// {0} - Address
        /// {1} - City
        /// {2} - StateProvinceId
        /// {3} - CountryId
        /// {4} - ZipPostalCode
        /// </remarks>
        private const string TAXRATE_KEY = "Nop.taxrate.id-{0}-{1}-{2}-{3}-{4}";

        #endregion

        #region Fields

        private readonly AvalaraImportManager _avalaraImportManager;
        private readonly AvalaraTaxSettings _avalaraTaxSettings;
        private readonly IAddressService _addressService;
        private readonly ICacheManager _cacheManager;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICountryService _countryService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGeoLookupService _geoLookupService;
        private readonly ILogger _logger;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly TaxSettings _taxSettings;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public AvalaraTaxProvider(AvalaraImportManager avalaraImportManager, 
            AvalaraTaxSettings avalaraTaxSettings,
            IAddressService addressService,
            ICacheManager cacheManager,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICountryService countryService,
            IGenericAttributeService genericAttributeService,
            IGeoLookupService geoLookupService,
            ILogger logger,
            IProductAttributeParser productAttributeParser,
            ISettingService settingService,
            IStoreContext storeContext,
            ITaxCategoryService taxCategoryService,
            TaxSettings taxSettings,
            IWebHelper webHelper)
        {
            this._avalaraImportManager = avalaraImportManager;
            this._avalaraTaxSettings = avalaraTaxSettings;
            this._addressService = addressService;
            this._cacheManager = cacheManager;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._countryService = countryService;
            this._genericAttributeService = genericAttributeService;
            this._geoLookupService = geoLookupService;
            this._logger = logger;
            this._productAttributeParser = productAttributeParser;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._taxCategoryService = taxCategoryService;
            this._taxSettings = taxSettings;
            this._webHelper = webHelper;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get address dictionary
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Dictionary of used addresses</returns>
        protected IDictionary<string, common.Address> GetAddressDictionary(Order order)
        {
            //get destination address
            var destinationAddress = GetDestinationAddress(order);

            //add destination, origin and billing addresses to dictionary
            return new Dictionary<string, common.Address>
            {
                { "origin", GetOriginAddress(order.StoreId) ?? destinationAddress },
                { "destination", destinationAddress },
                { "billing", order.BillingAddress },
            };

        }

        /// <summary>
        /// Get a tax destination address
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Address</returns>
        protected common.Address GetDestinationAddress(Order order)
        {
            common.Address destinationAddress = null;

            //tax is based on billing address
            if (_taxSettings.TaxBasedOn == TaxBasedOn.BillingAddress)
                destinationAddress = order.BillingAddress;

            //tax is based on shipping address
            if (_taxSettings.TaxBasedOn == TaxBasedOn.ShippingAddress)
                destinationAddress = order.ShippingAddress;

            //tax is based on pickup point address
            if (_taxSettings.TaxBasedOnPickupPointAddress && order.PickupAddress != null)
                destinationAddress = order.PickupAddress;

            //or use default address for tax calculation
            if (destinationAddress == null)
                destinationAddress = _addressService.GetAddressById(_taxSettings.DefaultTaxAddressId);

            return destinationAddress;
        }

        /// <summary>
        /// Get a tax origin address
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Address</returns>
        protected common.Address GetOriginAddress(int storeId)
        {
            //load settings for shipping origin address identifier
            var originAddressId = _settingService.GetSettingByKey<int>("ShippingSettings.ShippingOriginAddressId",
                storeId: storeId, loadSharedValueIfNotFound: true);

            //try get address that will be used for tax origin 
            return _addressService.GetAddressById(originAddressId);
        }

        /// <summary>
        /// Get item lines for the tax request
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="addresses">Address dictionary</param>
        /// <returns>List of item lines</returns>
        protected IList<Line> GetItemLines(Order order, IDictionary<string, common.Address> addresses)
        {
            //get destination and origin address codes
            var destinationCode = addresses["destination"]?.Id.ToString();
            var originCode = addresses["origin"]?.Id.ToString();

            //get purchased products details
            var items = CreateLinesForOrderItems(order, destinationCode, originCode).ToList();

            //set payment method additional fee as the separate item line
            if (order.PaymentMethodAdditionalFeeExclTax > decimal.Zero)
                items.Add(CreateLineForPaymentMethod(order, destinationCode, originCode));

            //set shipping rate as the separate item line
            if (order.OrderShippingExclTax > decimal.Zero)
                items.Add(CreateLineForShipping(order, destinationCode, originCode));

            //set checkout attributes as the separate item lines
            if (!string.IsNullOrEmpty(order.CheckoutAttributesXml))
                items.AddRange(CreateLinesForCheckoutAttributes(order, destinationCode, originCode));

            return items;
        }

        /// <summary>
        /// Create item lines for the tax request from order items
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="destinationCode">Destination address code</param>
        /// <param name="originCode">Origin address code</param>
        /// <returns>Collection of item lines</returns>
        protected IEnumerable<Line> CreateLinesForOrderItems(Order order, string destinationCode, string originCode)
        {
            return order.OrderItems.Select(orderItem =>
            {
                //create line
                var item = new Line
                {
                    Amount = orderItem.PriceExclTax,
                    DestinationCode = destinationCode,
                    Discounted = order.OrderSubTotalDiscountExclTax > decimal.Zero,
                    LineNo = orderItem.Id.ToString(),
                    OriginCode = originCode,
                    Qty = orderItem.Quantity
                };

                //whether to override destination address code
                if (UseEuVatRules(order, orderItem.Product))
                {
                    //use the billing address as the destination one in accordance with the EU VAT rules
                    item.DestinationCode = order.BillingAddress?.Id.ToString();
                }

                //set SKU as item code
                item.ItemCode = orderItem.Product?.FormatSku(orderItem.AttributesXml, _productAttributeParser);

                //item description
                item.Description = orderItem.Product?.ShortDescription ?? orderItem.Product?.Name;

                //set tax category as tax code
                var productTaxCategory = _taxCategoryService.GetTaxCategoryById(orderItem.Product?.TaxCategoryId ?? 0);
                item.TaxCode = GetTaxCodeByTaxCategory(productTaxCategory);

                ////try get product exemption
                //item.ExemptionNo = orderItem.Product != null && orderItem.Product.IsTaxExempt
                //    ? string.Format("Product-{0}", orderItem.Product.Id) : null;

                //as written in AvaTax documentation: You can find ExemptionNo in the GetTaxRequest at the document and line level.
                //but in fact, when you try to use it on line level you get the error: "Malformed JSON near 'ExemptionNo'"
                //so set CustomerUsageType to "L" - exempt by nopCommerce reason, it will enable the tax exempt
                if (orderItem.Product?.IsTaxExempt ?? false)
                    item.CustomerUsageType = CustomerUsageType.L;

                return item;
            });
        }

        /// <summary>
        /// Create item line for the tax request from payment method additional fee
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="destinationCode">Destination address code</param>
        /// <param name="originCode">Origin address code</param>
        /// <returns>Item line</returns>
        protected Line CreateLineForPaymentMethod(Order order, string destinationCode, string originCode)
        {
            //create line
            var paymentItem = new Line
            {
                Amount = order.PaymentMethodAdditionalFeeExclTax,
                Description = "Payment method additional fee",
                DestinationCode = destinationCode,
                ItemCode = order.PaymentMethodSystemName,
                LineNo = "payment",
                OriginCode = originCode,
                Qty = 1
            };

            //whether payment is taxable
            if (_taxSettings.PaymentMethodAdditionalFeeIsTaxable)
            {
                //try get tax code
                var paymentTaxCategory = _taxCategoryService.GetTaxCategoryById(_taxSettings.PaymentMethodAdditionalFeeTaxClassId);
                paymentItem.TaxCode = GetTaxCodeByTaxCategory(paymentTaxCategory);
            }
            else
            {
                //if payment was already taxed, set as exempt
                //paymentItem.ExemptionNo = "Payment-fee";

                //as written in AvaTax documentation: You can find ExemptionNo in the GetTaxRequest at the document and line level.
                //but in fact, when you try to use it on line level you get the error: "Malformed JSON near 'ExemptionNo'"
                //so set CustomerUsageType to "L" - exempt by nopCommerce reason, it will enable the tax exempt
                paymentItem.CustomerUsageType = CustomerUsageType.L;
            }

            return paymentItem;
        }

        /// <summary>
        /// Create item line for the tax request from shipping rate
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="destinationCode">Destination address code</param>
        /// <param name="originCode">Origin address code</param>
        /// <returns>Item line</returns>
        protected Line CreateLineForShipping(Order order, string destinationCode, string originCode)
        {
            //create line
            var shippingItem = new Line
            {
                Amount = order.OrderShippingExclTax,
                Description = "Shipping rate",
                DestinationCode = destinationCode,
                ItemCode = order.ShippingMethod,
                LineNo = "shipping",
                OriginCode = originCode,
                Qty = 1
            };

            //whether shipping is taxable
            if (_taxSettings.ShippingIsTaxable)
            {
                //try get tax code
                var shippingTaxCategory = _taxCategoryService.GetTaxCategoryById(_taxSettings.ShippingTaxClassId);
                shippingItem.TaxCode = GetTaxCodeByTaxCategory(shippingTaxCategory);
            }
            else
            {
                //if shipping was already taxed, set as exempt
                //shippingItem.ExemptionNo = "Shipping-rate";

                //as written in AvaTax documentation: You can find ExemptionNo in the GetTaxRequest at the document and line level.
                //but in fact, when you try to use it on line level you get the error: "Malformed JSON near 'ExemptionNo'"
                //so set CustomerUsageType to "L" - exempt by nopCommerce reason, it will enable the tax exempt
                shippingItem.CustomerUsageType = CustomerUsageType.L;
            }

            return shippingItem;
        }

        /// <summary>
        /// Create item lines for the tax request from checkout attributes
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="destinationCode">Destination address code</param>
        /// <param name="originCode">Origin address code</param>
        /// <returns>Collection of item lines</returns>
        protected IEnumerable<Line> CreateLinesForCheckoutAttributes(Order order, string destinationCode, string originCode)
        {
            //get checkout attributes values
            var attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(order.CheckoutAttributesXml);

            return attributeValues.Where(attributeValue => attributeValue.CheckoutAttribute != null).Select(attributeValue =>
            {
                //create line
                var checkoutAttributeItem = new Line
                {
                    Amount = attributeValue.PriceAdjustment,
                    Description = $"{attributeValue.CheckoutAttribute.Name} ({attributeValue.Name})",
                    DestinationCode = destinationCode,
                    Discounted = order.OrderSubTotalDiscountExclTax > decimal.Zero,
                    ItemCode = $"{attributeValue.CheckoutAttribute.Name}-{attributeValue.Name}",
                    LineNo = $"Checkout-{attributeValue.CheckoutAttribute.Name}",
                    OriginCode = originCode,
                    Qty = 1
                };

                //whether checkout attribute is tax exempt
                if (attributeValue.CheckoutAttribute.IsTaxExempt)
                {
                    //set item as exempt
                    //checkoutAttributeItem.ExemptionNo = "Checkout-attribute";

                    //as written in AvaTax documentation: You can find ExemptionNo in the GetTaxRequest at the document and line level.
                    //but in fact, when you try to use it on line level you get the error: "Malformed JSON near 'ExemptionNo'"
                    //so set CustomerUsageType to "L" - exempt by nopCommerce reason, it will enable the tax exempt
                    checkoutAttributeItem.CustomerUsageType = CustomerUsageType.L;
                }
                else
                { 
                    //try get tax code
                    var attributeTaxCategory = _taxCategoryService.GetTaxCategoryById(attributeValue.CheckoutAttribute.TaxCategoryId);
                    checkoutAttributeItem.TaxCode = GetTaxCodeByTaxCategory(attributeTaxCategory);
                }

                return checkoutAttributeItem;
            });
        }

        /// <summary>
        /// Get a value whether need to use European Union VAT rules for tax calculation
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="purchasedProduct">Purchased product</param>
        /// <returns>True if need to use EU VAT rules; otherwise false</returns>
        protected bool UseEuVatRules(Order order, Product purchasedProduct)
        {
            //whether EU VAT rules enabled and purchased product belongs to the telecommunications, broadcasting and electronic services
            return _taxSettings.EuVatEnabled
                && (purchasedProduct?.IsTelecommunicationsOrBroadcastingOrElectronicServices ?? false)
                && IsEuConsumer(order.Customer, order.BillingAddress);
        }

        /// <summary>
        /// Get a value indicating whether a customer is consumer located in Europe Union
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="billingAddress">Billing address</param>
        /// <returns>True if is EU consumer; otherwise false</returns>
        protected virtual bool IsEuConsumer(Customer customer, common.Address billingAddress)
        {
            //get country from billing address
            var country = billingAddress.Country;

            //get country specified during registration?
            if (country == null)
                country = _countryService.GetCountryById(customer.GetAttribute<int>(SystemCustomerAttributeNames.CountryId));

            //get country by IP address
            if (country == null)
                country = _countryService.GetCountryByTwoLetterIsoCode(_geoLookupService.LookupCountryIsoCode(customer.LastIpAddress));

            //we cannot detect country
            if (country == null)
                return false;

            //outside EU
            if (!country.SubjectToVat)
                return false;

            //company (business) or consumer?
            if ((VatNumberStatus)customer.GetAttribute<int>(SystemCustomerAttributeNames.VatNumberStatusId) == VatNumberStatus.Valid)
                return false;

            return true;
        }

        /// <summary>
        /// Get a whole request tax exeption
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Exemption reason</returns>
        protected string GetRequestExemption(Customer customer)
        {
            if (customer == null)
                return null;

            //customer tax exemption
            if (customer.IsTaxExempt)
                return CommonHelper.EnsureMaximumLength($"Exempt-customer-{customer.Id}", 25);

            //customer role tax exemption
            var exemptRole = customer.CustomerRoles.FirstOrDefault(role => role.Active && role.TaxExempt);

            return exemptRole != null ? CommonHelper.EnsureMaximumLength($"Exempt-{exemptRole.Name}", 25) : null;
        }

        /// <summary>
        /// Create the address for request from address entity
        /// </summary>
        /// <param name="address">Address entity</param>
        /// <returns>Address</returns>
        protected Address CreateAddress(common.Address address)
        {
            return new Address
            {
                AddressCode = address.Id.ToString(),
                Line1 = address.Address1,
                Line2 = address.Address2,
                City = address.City,
                Region = address.StateProvince?.Abbreviation,
                Country = address.Country?.TwoLetterIsoCode,
                PostalCode = address.ZipPostalCode
            };
        }

        /// <summary>
        /// Get tax code of the passed tax category
        /// </summary>
        /// <param name="taxCategory">Tax category</param>
        /// <returns>Tax code</returns>
        protected string GetTaxCodeByTaxCategory(TaxCategory taxCategory)
        {
            if (taxCategory == null)
                return null;

            //try to get tax code from the previously saved attribute
            var taxCode = taxCategory.GetAttribute<string>(_avalaraImportManager.AvaTaxCodeAttribute);
            if (!string.IsNullOrEmpty(taxCode))
                return taxCode;

            //or use the name as tax code
            return taxCategory.Name;
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
                return new CalculateTaxResult { Errors = new[] { "Address is not set" } };

            //construct a cache key
            var cacheKey = string.Format(TAXRATE_KEY,
                calculateTaxRequest.Address.Address1,
                calculateTaxRequest.Address.City,
                calculateTaxRequest.Address.StateProvince?.Id ?? 0,
                calculateTaxRequest.Address.Country?.Id ?? 0,
                calculateTaxRequest.Address.ZipPostalCode);

            // we don't use standard way _cacheManager.Get() due the need write errors to CalculateTaxResult
            if (_cacheManager.IsSet(cacheKey))
                return new CalculateTaxResult { TaxRate = _cacheManager.Get<decimal>(cacheKey) };

            //create a simplified tax request (only for get and the further use of tax rate)
            var taxRequest = new TaxRequest
            {
                Client = AvalaraTaxHelper.AVATAX_CLIENT,
                CompanyCode = _avalaraTaxSettings.CompanyCode,
                CustomerCode = calculateTaxRequest.Customer?.Id.ToString(),
                DetailLevel = DetailLevel.Tax,
                DocCode = Guid.NewGuid().ToString(),
                DocType = DocType.SalesOrder
            };

            //set destination and origin addresses
            var originAddress = GetOriginAddress(_storeContext.CurrentStore.Id) ?? calculateTaxRequest.Address;
            taxRequest.Addresses = new[]
            {
                CreateAddress(originAddress),
                CreateAddress(calculateTaxRequest.Address)
            };

            //create a simplified item line
            var line = new Line
            {
                LineNo = "1",
                DestinationCode = calculateTaxRequest.Address?.Id.ToString(),
                OriginCode = originAddress?.Id.ToString()
            };
            taxRequest.Lines = new[] { line };

            //get response
            var result = AvalaraTaxHelper.PostTaxRequest(taxRequest, _avalaraTaxSettings, _logger);
            if (result == null)
                return new CalculateTaxResult { Errors = new[] { "Bad request" } };
            
            //there are any errors
            if (result.ResultCode != SeverityLevel.Success)
                return new CalculateTaxResult { Errors = result.Messages.Select(message => message.Summary).ToList() };

            if (result.TaxLines == null || !result.TaxLines.Any())
                return new CalculateTaxResult { Errors = new[] { "Tax rates were not received" } };

            //tax rate successfully received, so cache it
            var taxRate = result.TaxLines[0].Rate * 100;
            _cacheManager.Set(cacheKey, taxRate, 60);

            return new CalculateTaxResult { TaxRate = taxRate };
        }

        /// <summary>
        /// Commit tax request to save it on AvaTax account history 
        /// </summary>
        /// <param name="order">Order</param>
        public void CommitTaxRequest(Order order)
        {
            GetTax(order, true);
        }

        /// <summary>
        /// Get tax details for the placed order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="commit">Whether to commit tax request (record on the AvaTax account history)</param>
        /// <returns>Tax details</returns>
        public TaxResponse GetTax(Order order, bool commit)
        {
            if (order.BillingAddress == null)
                return null;

            //create tax request
            var taxRequest = new TaxRequest
            {
                Commit = true,
                Client = AvalaraTaxHelper.AVATAX_CLIENT,
                CompanyCode = _avalaraTaxSettings.CompanyCode,
                CustomerCode = order.Customer?.Id.ToString(),
                CurrencyCode = order.CustomerCurrencyCode,
                DetailLevel = DetailLevel.Tax,
                Discount = order.OrderSubTotalDiscountExclTax,
                DocCode = commit ? order.CustomOrderNumber : order.OrderGuid.ToString(),
                DocType = commit && _avalaraTaxSettings.CommitTransactions ? DocType.SalesInvoice : DocType.SalesOrder,
                DocDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PurchaseOrderNo = commit ? order.CustomOrderNumber : order.OrderGuid.ToString()
            };

            //set addresses
            var addresses = GetAddressDictionary(order);
            taxRequest.Addresses = addresses.Values.Distinct().Select(CreateAddress).ToArray();

            //set purchased item lines
            var items = GetItemLines(order, addresses);
            taxRequest.Lines = items.ToArray();

            //set whole request tax exemption
            taxRequest.ExemptionNo = GetRequestExemption(order.Customer);

            return AvalaraTaxHelper.PostTaxRequest(taxRequest, _avalaraTaxSettings, _logger);
        }

        /// <summary>
        /// Void or delete an existing transaction record from the AvaTax system. 
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="deleted">Whether order was deleted</param>
        public void VoidTaxRequest(Order order, bool deleted)
        {
            var cancelRequest = new CancelTaxRequest
            {
                CancelCode = deleted ? CancelReason.DocDeleted : CancelReason.DocVoided,
                CompanyCode= _avalaraTaxSettings.CompanyCode,
                DocCode = order.CustomOrderNumber,
                DocType = DocType.SalesInvoice
            };

            AvalaraTaxHelper.CancelTaxRequest(cancelRequest, _avalaraTaxSettings, _logger);
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/TaxAvalara/Configure";
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
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Credentials.Declined", "Credentials declined");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Credentials.Verified", "Credentials verified");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.AccountId", "Account ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.AccountId.Hint", "Cpecify Avalara account ID.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.AvaTaxCode", "AvaTax tax code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.CommitTransactions", "Commit transactions");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.CommitTransactions.Hint", "Check for recording transactions in the history on your Avalara account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.CompanyCode", "Company code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.CompanyCode.Hint", "Enter your company code in Avalara account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.LicenseKey", "License key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.LicenseKey.Hint", "Cpecify Avalara account license key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.IsSandboxEnvironment", "Sandbox environment");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.IsSandboxEnvironment.Hint", "Check for using sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.ValidateAddresses", "Validate addresses");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.Fields.ValidateAddresses.Hint", "Check for validating addresses before tax requesting (only for US or Canadian address).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.ImportTaxCodes", "Import AvaTax tax codes");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.ImportTaxCodes.Success", "Successfully imported {0} tax codes");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.TestConnection", "Test connection");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.Avalara.TestTax", "Test tax calculation");
            
            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //tax codes
            foreach (var taxCategory in _taxCategoryService.GetAllTaxCategories())
            {
                _genericAttributeService.SaveAttribute<string>(taxCategory, _avalaraImportManager.AvaTaxCodeAttribute, null);
            }

            //settings
            _settingService.DeleteSetting<AvalaraTaxSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Credentials.Declined");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Credentials.Verified");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.AccountId");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.AccountId.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.AvaTaxCode");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.CommitTransactions");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.CommitTransactions.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.CompanyCode");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.CompanyCode.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.LicenseKey");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.LicenseKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.IsSandboxEnvironment");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.IsSandboxEnvironment.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.ValidateAddresses");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.Fields.ValidateAddresses.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.ImportTaxCodes");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.ImportTaxCodes.Success");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.TestConnection");
            this.DeletePluginLocaleResource("Plugins.Tax.Avalara.TestTax");

            base.Uninstall();
        }

        #endregion
    }
}
