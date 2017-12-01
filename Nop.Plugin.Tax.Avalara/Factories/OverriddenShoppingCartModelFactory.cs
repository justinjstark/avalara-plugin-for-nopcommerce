using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Http.Extensions;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Web.Factories;
using Nop.Web.Framework.Security.Captcha;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Plugin.Tax.Avalara.Factories
{
    /// <summary>
    /// Represents overridden shopping cart model factory
    /// </summary>
    public class OverriddenShoppingCartModelFactory : ShoppingCartModelFactory
    {
        #region Fields

        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly TaxSettings _taxSettings;

        #endregion

        #region Ctor

        public OverriddenShoppingCartModelFactory(IAddressModelFactory addressModelFactory,
            IStoreContext storeContext,
            IWorkContext workContext,
            IShoppingCartService shoppingCartService,
            IPictureService pictureService,
            ILocalizationService localizationService,
            IProductService productService,
            IProductAttributeFormatter productAttributeFormatter,
            IProductAttributeParser productAttributeParser,
            ITaxService taxService, ICurrencyService currencyService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IOrderProcessingService orderProcessingService,
            IDiscountService discountService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            IShippingService shippingService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ICheckoutAttributeService checkoutAttributeService,
            IPaymentService paymentService,
            IPermissionService permissionService,
            IDownloadService downloadService,
            IStaticCacheManager cacheManager,
            IWebHelper webHelper,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            MediaSettings mediaSettings,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            CommonSettings commonSettings,
            OrderSettings orderSettings,
            ShippingSettings shippingSettings,
            TaxSettings taxSettings,
            CaptchaSettings captchaSettings,
            AddressSettings addressSettings,
            RewardPointsSettings rewardPointsSettings,
            CustomerSettings customerSettings) : base(addressModelFactory,
                storeContext,
                workContext,
                shoppingCartService,
                pictureService,
                localizationService,
                productService,
                productAttributeFormatter,
                productAttributeParser,
                taxService,
                currencyService,
                priceCalculationService,
                priceFormatter,
                checkoutAttributeParser,
                checkoutAttributeFormatter,
                orderProcessingService,
                discountService,
                countryService,
                stateProvinceService,
                shippingService,
                orderTotalCalculationService,
                checkoutAttributeService,
                paymentService,
                permissionService,
                downloadService,
                cacheManager,
                webHelper,
                genericAttributeService,
                httpContextAccessor,
                mediaSettings,
                shoppingCartSettings,
                catalogSettings,
                commonSettings,
                orderSettings,
                shippingSettings,
                taxSettings,
                captchaSettings,
                addressSettings,
                rewardPointsSettings,
                customerSettings)
        {
            this._countryService = countryService;
            this._currencyService = currencyService;
            this._httpContextAccessor = httpContextAccessor;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._paymentService = paymentService;
            this._priceCalculationService = priceCalculationService;
            this._priceFormatter = priceFormatter;
            this._productAttributeParser = productAttributeParser;
            this._productService = productService;
            this._stateProvinceService = stateProvinceService;
            this._storeContext = storeContext;
            this._taxService = taxService;
            this._workContext = workContext;
            this._rewardPointsSettings = rewardPointsSettings;
            this._shippingSettings = shippingSettings;
            this._taxSettings = taxSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get tax total by Avalara tax service
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <param name="shippingMethodName">Shipping method name</param>
        /// <param name="paymentMethodAdditionalFee">Payment method additional fee</param>
        /// <returns>Tax total</returns>
        private decimal? GetTaxTotal(IList<ShoppingCartItem> cart, string shippingMethodName, decimal paymentMethodAdditionalFee)
        {
            //ensure that Avalara tax provider is active
            if (!(_taxService.LoadActiveTaxProvider(_workContext.CurrentCustomer) is AvalaraTaxProvider taxProvider))
                return null;

            //get order payment info
            var processPaymentRequest = _httpContextAccessor.HttpContext.Session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
            if (processPaymentRequest == null)
                return null;

            //create dummy order for the tax request
            var order = new Order
            {
                BillingAddress = _workContext.CurrentCustomer.BillingAddress,
                CheckoutAttributesXml = _workContext.CurrentCustomer
                    .GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, processPaymentRequest.StoreId),
                Customer = _workContext.CurrentCustomer,
                OrderGuid = processPaymentRequest.OrderGuid,
                OrderShippingExclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, false) ?? 0,
                PaymentMethodAdditionalFeeExclTax = _taxService
                    .GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, false, _workContext.CurrentCustomer),
                PaymentMethodSystemName = processPaymentRequest.PaymentMethodSystemName,
                ShippingAddress = _workContext.CurrentCustomer.ShippingAddress,
                ShippingMethod = shippingMethodName
            };

            //add pickup address
            if (_shippingSettings.AllowPickUpInStore)
            {
                var pickupPoint = _workContext.CurrentCustomer.GetAttribute<PickupPoint>(SystemCustomerAttributeNames.SelectedPickupPoint, processPaymentRequest.StoreId);
                if (pickupPoint != null)
                {
                    var country = _countryService.GetCountryByTwoLetterIsoCode(pickupPoint.CountryCode);
                    order.PickupAddress = new Address
                    {
                        Address1 = pickupPoint.Address,
                        City = pickupPoint.City,
                        Country = country,
                        StateProvince = _stateProvinceService.GetStateProvinceByAbbreviation(pickupPoint.StateAbbreviation, country?.Id),
                        ZipPostalCode = pickupPoint.ZipPostalCode,
                        CreatedOnUtc = DateTime.UtcNow,
                    };
                }
            }

            //add discount amount
            _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false, out decimal orderSubTotalDiscountExclTax, out _, out _, out _);
            order.OrderSubTotalDiscountExclTax = orderSubTotalDiscountExclTax;

            //create dummy order items
            foreach (var cartItem in cart)
            {
                order.OrderItems.Add(new OrderItem
                {
                    AttributesXml = cartItem.AttributesXml,
                    PriceExclTax = _taxService.GetProductPrice(cartItem.Product,
                        _priceCalculationService.GetSubTotal(cartItem, true, out _, out _, out _), false, _workContext.CurrentCustomer, out _),
                    Product = cartItem.Product,
                    Quantity = cartItem.Quantity
                });
            }

            //get tax total
            var taxTotal = taxProvider.GetOrderTax(order);

            //remove old value
            if (processPaymentRequest.CustomValues.ContainsKey(AvalaraTaxDefaults.TaxTotalCustomValue))
                processPaymentRequest.CustomValues.Remove(AvalaraTaxDefaults.TaxTotalCustomValue);

            //save it for the further usage
            if (taxTotal.HasValue)
            {
                processPaymentRequest.CustomValues.Add(AvalaraTaxDefaults.TaxTotalCustomValue, taxTotal.Value);
                _httpContextAccessor.HttpContext.Session.Set("OrderPaymentInfo", processPaymentRequest);
            }

            return taxTotal;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare the order totals model
        /// </summary>
        /// <param name="cart">List of the shopping cart item</param>
        /// <param name="isEditable">Whether model is editable</param>
        /// <returns>Order totals model</returns>
        public override OrderTotalsModel PrepareOrderTotalsModel(IList<ShoppingCartItem> cart, bool isEditable)
        {
            var model = new OrderTotalsModel
            {
                IsEditable = isEditable
            };

            if (cart.Any())
            {
                //subtotal
                var subTotalIncludingTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal;
                _orderTotalCalculationService.GetShoppingCartSubTotal(cart, subTotalIncludingTax, out decimal orderSubTotalDiscountAmountBase, out List<DiscountForCaching> _, out decimal subTotalWithoutDiscountBase, out decimal _);
                var subtotalBase = subTotalWithoutDiscountBase;
                var subtotal = _currencyService.ConvertFromPrimaryStoreCurrency(subtotalBase, _workContext.WorkingCurrency);
                model.SubTotal = _priceFormatter.FormatPrice(subtotal, true, _workContext.WorkingCurrency, _workContext.WorkingLanguage, subTotalIncludingTax);

                if (orderSubTotalDiscountAmountBase > decimal.Zero)
                {
                    var orderSubTotalDiscountAmount = _currencyService.ConvertFromPrimaryStoreCurrency(orderSubTotalDiscountAmountBase, _workContext.WorkingCurrency);
                    model.SubTotalDiscount = _priceFormatter.FormatPrice(-orderSubTotalDiscountAmount, true, _workContext.WorkingCurrency, _workContext.WorkingLanguage, subTotalIncludingTax);
                }


                //shipping info
                model.RequiresShipping = cart.RequiresShipping(_productService, _productAttributeParser);
                if (model.RequiresShipping)
                {
                    var shoppingCartShippingBase = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart);
                    if (shoppingCartShippingBase.HasValue)
                    {
                        var shoppingCartShipping = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartShippingBase.Value, _workContext.WorkingCurrency);
                        model.Shipping = _priceFormatter.FormatShippingPrice(shoppingCartShipping, true);

                        //selected shipping method
                        var shippingOption = _workContext.CurrentCustomer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, _storeContext.CurrentStore.Id);
                        if (shippingOption != null)
                            model.SelectedShippingMethod = shippingOption.Name;
                    }
                }
                else
                {
                    model.HideShippingTotal = _shippingSettings.HideShippingTotal;
                }

                //payment method fee
                var paymentMethodSystemName = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.SelectedPaymentMethod, _storeContext.CurrentStore.Id);
                var paymentMethodAdditionalFee = _paymentService.GetAdditionalHandlingFee(cart, paymentMethodSystemName);
                var paymentMethodAdditionalFeeWithTaxBase = _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, _workContext.CurrentCustomer);
                if (paymentMethodAdditionalFeeWithTaxBase > decimal.Zero)
                {
                    var paymentMethodAdditionalFeeWithTax = _currencyService.ConvertFromPrimaryStoreCurrency(paymentMethodAdditionalFeeWithTaxBase, _workContext.WorkingCurrency);
                    model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeWithTax, true);
                }

                //get tax total from the Avalara tax service, it may slightly differ from the original calculated tax 
                var taxTotal = GetTaxTotal(cart, model.SelectedShippingMethod, paymentMethodAdditionalFee);
                var adjustOrderTotal = 0M;

                //tax
                var displayTax = true;
                var displayTaxRates = true;
                if (_taxSettings.HideTaxInOrderSummary && _workContext.TaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    var shoppingCartTaxBase = _orderTotalCalculationService.GetTaxTotal(cart, out SortedDictionary<decimal, decimal> taxRates);
                    if (taxTotal.HasValue)
                    {
                        //adjust tax and order total according to received value from the Avalara
                        adjustOrderTotal = taxTotal.Value - shoppingCartTaxBase;
                        shoppingCartTaxBase = taxTotal.Value;
                    }

                    var shoppingCartTax = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartTaxBase, _workContext.WorkingCurrency);
                    if (shoppingCartTaxBase == 0 && _taxSettings.HideZeroTax)
                    {
                        displayTax = false;
                        displayTaxRates = false;
                    }
                    else
                    {
                        displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                        displayTax = !displayTaxRates;

                        model.Tax = _priceFormatter.FormatPrice(shoppingCartTax, true, false);
                        foreach (var tr in taxRates)
                        {
                            model.TaxRates.Add(new OrderTotalsModel.TaxRate
                            {
                                Rate = _priceFormatter.FormatTaxRate(tr.Key),
                                Value = _priceFormatter.FormatPrice(_currencyService.ConvertFromPrimaryStoreCurrency(tr.Value, _workContext.WorkingCurrency), true, false),
                            });
                        }
                    }
                }
                model.DisplayTaxRates = displayTaxRates;
                model.DisplayTax = displayTax;

                //total
                var shoppingCartTotalBase = _orderTotalCalculationService.GetShoppingCartTotal(cart, out decimal orderTotalDiscountAmountBase, out List<DiscountForCaching> _, out List<AppliedGiftCard> appliedGiftCards, out int redeemedRewardPoints, out decimal redeemedRewardPointsAmount);
                if (shoppingCartTotalBase.HasValue)
                {
                    //adjust order total according to received tax total from the Avalara
                    shoppingCartTotalBase += adjustOrderTotal;
                    var shoppingCartTotal = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartTotalBase.Value, _workContext.WorkingCurrency);
                    model.OrderTotal = _priceFormatter.FormatPrice(shoppingCartTotal, true, false);
                }

                //discount
                if (orderTotalDiscountAmountBase > decimal.Zero)
                {
                    var orderTotalDiscountAmount = _currencyService.ConvertFromPrimaryStoreCurrency(orderTotalDiscountAmountBase, _workContext.WorkingCurrency);
                    model.OrderTotalDiscount = _priceFormatter.FormatPrice(-orderTotalDiscountAmount, true, false);
                }

                //gift cards
                if (appliedGiftCards != null && appliedGiftCards.Any())
                {
                    foreach (var appliedGiftCard in appliedGiftCards)
                    {
                        var gcModel = new OrderTotalsModel.GiftCard
                        {
                            Id = appliedGiftCard.GiftCard.Id,
                            CouponCode = appliedGiftCard.GiftCard.GiftCardCouponCode,
                        };
                        var amountCanBeUsed = _currencyService.ConvertFromPrimaryStoreCurrency(appliedGiftCard.AmountCanBeUsed, _workContext.WorkingCurrency);
                        gcModel.Amount = _priceFormatter.FormatPrice(-amountCanBeUsed, true, false);

                        var remainingAmountBase = appliedGiftCard.GiftCard.GetGiftCardRemainingAmount() - appliedGiftCard.AmountCanBeUsed;
                        var remainingAmount = _currencyService.ConvertFromPrimaryStoreCurrency(remainingAmountBase, _workContext.WorkingCurrency);
                        gcModel.Remaining = _priceFormatter.FormatPrice(remainingAmount, true, false);

                        model.GiftCards.Add(gcModel);
                    }
                }

                //reward points to be spent (redeemed)
                if (redeemedRewardPointsAmount > decimal.Zero)
                {
                    var redeemedRewardPointsAmountInCustomerCurrency = _currencyService.ConvertFromPrimaryStoreCurrency(redeemedRewardPointsAmount, _workContext.WorkingCurrency);
                    model.RedeemedRewardPoints = redeemedRewardPoints;
                    model.RedeemedRewardPointsAmount = _priceFormatter.FormatPrice(-redeemedRewardPointsAmountInCustomerCurrency, true, false);
                }

                //reward points to be earned
                if (_rewardPointsSettings.Enabled &&
                    _rewardPointsSettings.DisplayHowMuchWillBeEarned &&
                    shoppingCartTotalBase.HasValue)
                {
                    var shippingBaseInclTax = model.RequiresShipping
                        ? _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, true)
                        : 0;
                    if (shippingBaseInclTax.HasValue)
                    {
                        var totalForRewardPoints = _orderTotalCalculationService.CalculateApplicableOrderTotalForRewardPoints(shippingBaseInclTax.Value, shoppingCartTotalBase.Value);
                        model.WillEarnRewardPoints = _orderTotalCalculationService.CalculateRewardPoints(_workContext.CurrentCustomer, totalForRewardPoints);
                    }
                }

            }

            return model;
        }

        #endregion
    }
}