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
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Tax.Avalara.Domain;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Factories;
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
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGiftCardService _giftCardService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly TaxSettings _taxSettings;

        #endregion

        #region Ctor

        public OverriddenShoppingCartModelFactory(AddressSettings addressSettings,
            CaptchaSettings captchaSettings,
            CatalogSettings catalogSettings,
            CommonSettings commonSettings,
            CustomerSettings customerSettings,
            IAddressModelFactory addressModelFactory,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICheckoutAttributeService checkoutAttributeService,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IDiscountService discountService,
            IDownloadService downloadService,
            IGenericAttributeService genericAttributeService,
            IGiftCardService giftCardService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IOrderProcessingService orderProcessingService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPaymentService paymentService,
            IPermissionService permissionService,
            IPictureService pictureService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeFormatter productAttributeFormatter,
            IProductService productService,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IStateProvinceService stateProvinceService,
            IStaticCacheManager cacheManager,
            IStoreContext storeContext,
            ITaxService taxService,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            OrderSettings orderSettings,
            RewardPointsSettings rewardPointsSettings,
            ShippingSettings shippingSettings,
            ShoppingCartSettings shoppingCartSettings,
            TaxSettings taxSettings,
            VendorSettings vendorSettings) : base(addressSettings,
                captchaSettings,
                catalogSettings,
                commonSettings,
                customerSettings,
                addressModelFactory,
                checkoutAttributeFormatter,
                checkoutAttributeParser,
                checkoutAttributeService,
                countryService,
                currencyService,
                customerService,
                discountService,
                downloadService,
                genericAttributeService,
                giftCardService,
                httpContextAccessor,
                localizationService,
                orderProcessingService,
                orderTotalCalculationService,
                paymentService,
                permissionService,
                pictureService,
                priceCalculationService,
                priceFormatter,
                productAttributeFormatter,
                productService,
                shippingService,
                shoppingCartService,
                stateProvinceService,
                cacheManager,
                storeContext,
                taxService,
                urlRecordService,
                vendorService,
                webHelper,
                workContext,
                mediaSettings,
                orderSettings,
                rewardPointsSettings,
                shippingSettings,
                shoppingCartSettings,
                taxSettings,
                vendorSettings)
        {
            this._countryService = countryService;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._giftCardService = giftCardService;
            this._httpContextAccessor = httpContextAccessor;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._paymentService = paymentService;
            this._priceCalculationService = priceCalculationService;
            this._priceFormatter = priceFormatter;
            this._shoppingCartService = shoppingCartService;
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
        /// Prepare tax details by Avalara tax service
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        private void PrepareTaxDetails(IList<ShoppingCartItem> cart)
        {
            //ensure that Avalara tax provider is active
            if (!(_taxService.LoadActiveTaxProvider(_workContext.CurrentCustomer) is AvalaraTaxProvider taxProvider))
                return;

            //create dummy order for the tax request
            var order = new Order { Customer = _workContext.CurrentCustomer };

            //addresses
            order.BillingAddress = _workContext.CurrentCustomer.BillingAddress;
            order.ShippingAddress = _workContext.CurrentCustomer.ShippingAddress;
            if (_shippingSettings.AllowPickUpInStore)
            {
                var pickupPoint = _genericAttributeService.GetAttribute<PickupPoint>(_workContext.CurrentCustomer,
                    NopCustomerDefaults.SelectedPickupPointAttribute, _storeContext.CurrentStore.Id);
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

            //checkout attributes
            order.CheckoutAttributesXml = _genericAttributeService.GetAttribute<string>(_workContext.CurrentCustomer,
                NopCustomerDefaults.CheckoutAttributes, _storeContext.CurrentStore.Id);

            //shipping method
            order.OrderShippingExclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, false) ?? 0;
            order.ShippingMethod = _genericAttributeService.GetAttribute<ShippingOption>(_workContext.CurrentCustomer,
                NopCustomerDefaults.SelectedShippingOptionAttribute, _storeContext.CurrentStore.Id)?.Name;

            //payment method
            var paymentMethod = _genericAttributeService.GetAttribute<string>(_workContext.CurrentCustomer,
                NopCustomerDefaults.SelectedPaymentMethodAttribute, _storeContext.CurrentStore.Id);
            var paymentFee = _paymentService.GetAdditionalHandlingFee(cart, paymentMethod);
            order.PaymentMethodAdditionalFeeExclTax = _taxService.GetPaymentMethodAdditionalFee(paymentFee, false, _workContext.CurrentCustomer);
            order.PaymentMethodSystemName = paymentMethod;

            //add discount amount
            _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false, out var orderSubTotalDiscountExclTax, out _, out _, out _);
            order.OrderSubTotalDiscountExclTax = orderSubTotalDiscountExclTax;

            //create dummy order items
            foreach (var cartItem in cart)
            {
                var orderItem = new OrderItem
                {
                    AttributesXml = cartItem.AttributesXml,
                    Product = cartItem.Product,
                    Quantity = cartItem.Quantity
                };

                var itemSubtotal = _priceCalculationService.GetSubTotal(cartItem, true, out _, out _, out _);
                orderItem.PriceExclTax = _taxService.GetProductPrice(cartItem.Product, itemSubtotal, false, _workContext.CurrentCustomer, out _);

                order.OrderItems.Add(orderItem);
            }

            //get tax details
            var taxTransaction = taxProvider.CreateOrderTaxTransaction(order, false);
            if (taxTransaction == null)
                return;

            //and save it for the further usage
            var taxDetails = new TaxDetails { TaxTotal = taxTransaction.totalTax };
            foreach (var item in taxTransaction.summary)
            {
                if (!item.rate.HasValue || !item.tax.HasValue)
                    continue;

                var taxRate = item.rate.Value * 100;
                var taxValue = item.tax.Value;

                if (!taxDetails.TaxRates.ContainsKey(taxRate))
                    taxDetails.TaxRates.Add(taxRate, taxValue);
                else
                    taxDetails.TaxRates[taxRate] = taxDetails.TaxRates[taxRate] + taxValue;
            }
            _httpContextAccessor.HttpContext.Session.Set(AvalaraTaxDefaults.TaxDetailsSessionValue, taxDetails);
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
                //Avalara plugin changes
                PrepareTaxDetails(cart);
                //Avalara plugin changes

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
                model.RequiresShipping = _shoppingCartService.ShoppingCartRequiresShipping(cart);
                if (model.RequiresShipping)
                {
                    var shoppingCartShippingBase = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart);
                    if (shoppingCartShippingBase.HasValue)
                    {
                        var shoppingCartShipping = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartShippingBase.Value, _workContext.WorkingCurrency);
                        model.Shipping = _priceFormatter.FormatShippingPrice(shoppingCartShipping, true);

                        //selected shipping method
                        var shippingOption = _genericAttributeService.GetAttribute<ShippingOption>(_workContext.CurrentCustomer,
                            NopCustomerDefaults.SelectedShippingOptionAttribute, _storeContext.CurrentStore.Id);
                        if (shippingOption != null)
                            model.SelectedShippingMethod = shippingOption.Name;
                    }
                }
                else
                {
                    model.HideShippingTotal = _shippingSettings.HideShippingTotal;
                }

                //payment method fee
                var paymentMethodSystemName = _genericAttributeService.GetAttribute<string>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPaymentMethodAttribute, _storeContext.CurrentStore.Id);
                var paymentMethodAdditionalFee = _paymentService.GetAdditionalHandlingFee(cart, paymentMethodSystemName);
                var paymentMethodAdditionalFeeWithTaxBase = _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, _workContext.CurrentCustomer);
                if (paymentMethodAdditionalFeeWithTaxBase > decimal.Zero)
                {
                    var paymentMethodAdditionalFeeWithTax = _currencyService.ConvertFromPrimaryStoreCurrency(paymentMethodAdditionalFeeWithTaxBase, _workContext.WorkingCurrency);
                    model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeWithTax, true);
                }
                
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

                    //Avalara plugin changes
                    //get tax details from the Avalara tax service, it may slightly differ from the original calculated tax
                    var taxDetails = _httpContextAccessor.HttpContext.Session.Get<TaxDetails>(AvalaraTaxDefaults.TaxDetailsSessionValue);
                    if (taxDetails != null)
                    {
                        //adjust tax total according to received value from the Avalara
                        if (taxDetails.TaxTotal.HasValue)
                            shoppingCartTaxBase = taxDetails.TaxTotal.Value;

                        if (taxDetails.TaxRates?.Any() ?? false)
                            taxRates = new SortedDictionary<decimal, decimal>(taxDetails.TaxRates);
                    }
                    //Avalara plugin changes

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

                        var remainingAmountBase = _giftCardService.GetGiftCardRemainingAmount(appliedGiftCard.GiftCard) - appliedGiftCard.AmountCanBeUsed;
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
                if (_rewardPointsSettings.Enabled && _rewardPointsSettings.DisplayHowMuchWillBeEarned && shoppingCartTotalBase.HasValue)
                {
                    //get shipping total
                    var shippingBaseInclTax = !model.RequiresShipping ? 0 : _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, true) ?? 0;

                    //get total for reward points
                    var totalForRewardPoints = _orderTotalCalculationService
                        .CalculateApplicableOrderTotalForRewardPoints(shippingBaseInclTax, shoppingCartTotalBase.Value);
                    if (totalForRewardPoints > decimal.Zero)
                        model.WillEarnRewardPoints = _orderTotalCalculationService.CalculateRewardPoints(_workContext.CurrentCustomer, totalForRewardPoints);
                }

            }

            return model;
        }

        #endregion
    }
}