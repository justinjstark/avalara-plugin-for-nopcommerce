using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Tax.Avalara.Domain;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Vendors;

namespace Nop.Plugin.Tax.Avalara.Services
{
    public class AvalaraOrderProcessingService : OrderProcessingService
    {
        #region Fields

        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly IGiftCardService _giftCardService;
        private readonly IDiscountService _discountService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly PaymentSettings _paymentSettings;

        #endregion

        #region Ctor

        public AvalaraOrderProcessingService(IOrderService orderService,
            IWebHelper webHelper,
            ILocalizationService localizationService,
            ILanguageService languageService,
            IProductService productService,
            IPaymentService paymentService,
            ILogger logger,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeFormatter productAttributeFormatter,
            IGiftCardService giftCardService,
            IShoppingCartService shoppingCartService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IShippingService shippingService,
            IShipmentService shipmentService,
            ITaxService taxService,
            ICustomerService customerService,
            IDiscountService discountService,
            IEncryptionService encryptionService,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            IVendorService vendorService,
            ICustomerActivityService customerActivityService,
            ICurrencyService currencyService,
            IAffiliateService affiliateService,
            IEventPublisher eventPublisher,
            IPdfService pdfService,
            IRewardPointService rewardPointService,
            IGenericAttributeService genericAttributeService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            ShippingSettings shippingSettings,
            PaymentSettings paymentSettings,
            RewardPointsSettings rewardPointsSettings,
            OrderSettings orderSettings,
            TaxSettings taxSettings,
            LocalizationSettings localizationSettings,
            CurrencySettings currencySettings,
            ICustomNumberFormatter customNumberFormatter) : base(orderService,
                webHelper,
                localizationService,
                languageService,
                productService,
                paymentService,
                logger,
                orderTotalCalculationService,
                priceCalculationService,
                priceFormatter,
                productAttributeParser,
                productAttributeFormatter,
                giftCardService,
                shoppingCartService,
                checkoutAttributeFormatter,
                shippingService,
                shipmentService,
                taxService,
                customerService,
                discountService,
                encryptionService,
                workContext,
                workflowMessageService,
                vendorService,
                customerActivityService,
                currencyService,
                affiliateService,
                eventPublisher,
                pdfService,
                rewardPointService,
                genericAttributeService,
                countryService,
                stateProvinceService,
                shippingSettings,
                paymentSettings,
                rewardPointsSettings,
                orderSettings,
                taxSettings,
                localizationSettings,
                currencySettings,
                customNumberFormatter)
        {
            this._customerActivityService = customerActivityService;
            this._customerService = customerService;
            this._giftCardService = giftCardService;
            this._discountService = discountService;
            this._eventPublisher = eventPublisher;
            this._localizationService = localizationService;
            this._logger = logger;
            this._orderService = orderService;
            this._paymentService = paymentService;
            this._priceCalculationService = priceCalculationService;
            this._productAttributeFormatter = productAttributeFormatter;
            this._productAttributeParser = productAttributeParser;
            this._productService = productService;
            this._shippingService = shippingService;
            this._shoppingCartService = shoppingCartService;
            this._taxService = taxService;
            this._workContext = workContext;
            this._paymentSettings = paymentSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get tax total from Avalara tax service
        /// </summary>
        /// <param name="processPaymentRequest">Process payment request</param>
        /// <param name="details">Details</param>
        /// <returns>Tax total</returns>
        protected decimal? GetTaxTotal(ProcessPaymentRequest processPaymentRequest, PlaceOrderContainer details)
        {
            //ensure that Avalara tax rate provider is active
            var taxProvider = _taxService.LoadActiveTaxProvider(details.Customer) as AvalaraTaxProvider;
            if (taxProvider == null)
                return null;

            //create fake order for the request
            var order = new Order
            {
                BillingAddress = details.BillingAddress,
                CheckoutAttributesXml = details.CheckoutAttributesXml,
                Customer = details.Customer,
                CustomerCurrencyCode = details.CustomerCurrencyCode,
                CustomOrderNumber = string.Empty,
                OrderGuid = processPaymentRequest.OrderGuid,
                OrderShippingExclTax = details.OrderShippingTotalExclTax,
                OrderSubTotalDiscountExclTax = details.OrderSubTotalDiscountExclTax,
                PaymentMethodAdditionalFeeExclTax = details.PaymentAdditionalFeeExclTax,
                PaymentMethodSystemName = processPaymentRequest.PaymentMethodSystemName,
                PickupAddress = details.PickupAddress,
                ShippingAddress = details.ShippingAddress,
                ShippingMethod = details.ShippingMethodName,
                StoreId = processPaymentRequest.StoreId,
            };

            var idCount = 0;
            foreach (var cartItem in details.Cart)
            {
                decimal taxRate;
                List<DiscountForCaching> scDiscounts;
                decimal discountAmount;
                int? maximumDiscountQty;
                var scSubTotal = _priceCalculationService.GetSubTotal(cartItem, true, out discountAmount, out scDiscounts, out maximumDiscountQty);
                var scSubTotalExclTax = _taxService.GetProductPrice(cartItem.Product, scSubTotal, false, details.Customer, out taxRate);
                order.OrderItems.Add(new OrderItem
                {
                    AttributesXml = cartItem.AttributesXml,
                    Id = ++idCount,
                    PriceExclTax = scSubTotalExclTax,
                    Product = cartItem.Product,
                    Quantity = cartItem.Quantity
                });
            }

            //get tax details
            var result = taxProvider.GetTax(order, false);
            if (result == null || result.ResultCode != SeverityLevel.Success)
                return null;

            return result.TotalTax;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Places an order
        /// </summary>
        /// <param name="processPaymentRequest">Process payment request</param>
        /// <returns>Place order result</returns>
        public override PlaceOrderResult PlaceOrder(ProcessPaymentRequest processPaymentRequest)
        {
            //ensure that Avalara tax rate provider is active
            var taxProvider = _taxService.LoadActiveTaxProvider(_workContext.CurrentCustomer) as AvalaraTaxProvider;
            if (taxProvider == null)
                return base.PlaceOrder(processPaymentRequest);

            if (processPaymentRequest == null)
                throw new ArgumentNullException(nameof(processPaymentRequest));

            var result = new PlaceOrderResult();
            try
            {
                if (processPaymentRequest.OrderGuid == Guid.Empty)
                    processPaymentRequest.OrderGuid = Guid.NewGuid();

                //prepare order details
                var details = PreparePlaceOrderDetails(processPaymentRequest);

                //the only difference from the original PlaceOrder() method
                //get tax total from the Avalara tax service, it may slightly differ from the previously calculated tax 
                var taxTotal = GetTaxTotal(processPaymentRequest, details);
                if (taxTotal.HasValue)
                {
                    details.OrderTotal += (taxTotal.Value - details.OrderTaxTotal);
                    processPaymentRequest.OrderTotal = details.OrderTotal;
                    details.OrderTaxTotal = taxTotal.Value;
                }

                #region Payment workflow


                //process payment
                ProcessPaymentResult processPaymentResult;
                //skip payment workflow if order total equals zero
                var skipPaymentWorkflow = details.OrderTotal == decimal.Zero;
                if (!skipPaymentWorkflow)
                {
                    var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(processPaymentRequest.PaymentMethodSystemName);
                    if (paymentMethod == null)
                        throw new NopException("Payment method couldn't be loaded");

                    //ensure that payment method is active
                    if (!paymentMethod.IsPaymentMethodActive(_paymentSettings))
                        throw new NopException("Payment method is not active");

                    if (details.IsRecurringShoppingCart)
                    {
                        //recurring cart
                        switch (_paymentService.GetRecurringPaymentType(processPaymentRequest.PaymentMethodSystemName))
                        {
                            case RecurringPaymentType.NotSupported:
                                throw new NopException("Recurring payments are not supported by selected payment method");
                            case RecurringPaymentType.Manual:
                            case RecurringPaymentType.Automatic:
                                processPaymentResult = _paymentService.ProcessRecurringPayment(processPaymentRequest);
                                break;
                            default:
                                throw new NopException("Not supported recurring payment type");
                        }
                    }
                    else
                        //standard cart
                        processPaymentResult = _paymentService.ProcessPayment(processPaymentRequest);
                }
                else
                    //payment is not required
                    processPaymentResult = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Paid };

                if (processPaymentResult == null)
                    throw new NopException("processPaymentResult is not available");

                #endregion

                if (processPaymentResult.Success)
                {
                    #region Save order details

                    var order = SaveOrderDetails(processPaymentRequest, processPaymentResult, details);
                    result.PlacedOrder = order;

                    //move shopping cart items to order items
                    foreach (var sc in details.Cart)
                    {
                        //prices
                        decimal taxRate;
                        List<DiscountForCaching> scDiscounts;
                        decimal discountAmount;
                        int? maximumDiscountQty;
                        var scUnitPrice = _priceCalculationService.GetUnitPrice(sc);
                        var scSubTotal = _priceCalculationService.GetSubTotal(sc, true, out discountAmount, out scDiscounts, out maximumDiscountQty);
                        var scUnitPriceInclTax = _taxService.GetProductPrice(sc.Product, scUnitPrice, true, details.Customer, out taxRate);
                        var scUnitPriceExclTax = _taxService.GetProductPrice(sc.Product, scUnitPrice, false, details.Customer, out taxRate);
                        var scSubTotalInclTax = _taxService.GetProductPrice(sc.Product, scSubTotal, true, details.Customer, out taxRate);
                        var scSubTotalExclTax = _taxService.GetProductPrice(sc.Product, scSubTotal, false, details.Customer, out taxRate);
                        var discountAmountInclTax = _taxService.GetProductPrice(sc.Product, discountAmount, true, details.Customer, out taxRate);
                        var discountAmountExclTax = _taxService.GetProductPrice(sc.Product, discountAmount, false, details.Customer, out taxRate);
                        foreach (var disc in scDiscounts)
                            if (!details.AppliedDiscounts.ContainsDiscount(disc))
                                details.AppliedDiscounts.Add(disc);

                        //attributes
                        var attributeDescription = _productAttributeFormatter.FormatAttributes(sc.Product, sc.AttributesXml, details.Customer);

                        var itemWeight = _shippingService.GetShoppingCartItemWeight(sc);

                        //save order item
                        var orderItem = new OrderItem
                        {
                            OrderItemGuid = Guid.NewGuid(),
                            Order = order,
                            ProductId = sc.ProductId,
                            UnitPriceInclTax = scUnitPriceInclTax,
                            UnitPriceExclTax = scUnitPriceExclTax,
                            PriceInclTax = scSubTotalInclTax,
                            PriceExclTax = scSubTotalExclTax,
                            OriginalProductCost = _priceCalculationService.GetProductCost(sc.Product, sc.AttributesXml),
                            AttributeDescription = attributeDescription,
                            AttributesXml = sc.AttributesXml,
                            Quantity = sc.Quantity,
                            DiscountAmountInclTax = discountAmountInclTax,
                            DiscountAmountExclTax = discountAmountExclTax,
                            DownloadCount = 0,
                            IsDownloadActivated = false,
                            LicenseDownloadId = 0,
                            ItemWeight = itemWeight,
                            RentalStartDateUtc = sc.RentalStartDateUtc,
                            RentalEndDateUtc = sc.RentalEndDateUtc
                        };
                        order.OrderItems.Add(orderItem);
                        _orderService.UpdateOrder(order);

                        //gift cards
                        if (sc.Product.IsGiftCard)
                        {
                            string giftCardRecipientName;
                            string giftCardRecipientEmail;
                            string giftCardSenderName;
                            string giftCardSenderEmail;
                            string giftCardMessage;
                            _productAttributeParser.GetGiftCardAttribute(sc.AttributesXml, out giftCardRecipientName,
                                out giftCardRecipientEmail, out giftCardSenderName, out giftCardSenderEmail, out giftCardMessage);

                            for (var i = 0; i < sc.Quantity; i++)
                            {
                                _giftCardService.InsertGiftCard(new GiftCard
                                {
                                    GiftCardType = sc.Product.GiftCardType,
                                    PurchasedWithOrderItem = orderItem,
                                    Amount = sc.Product.OverriddenGiftCardAmount.HasValue ? sc.Product.OverriddenGiftCardAmount.Value : scUnitPriceExclTax,
                                    IsGiftCardActivated = false,
                                    GiftCardCouponCode = _giftCardService.GenerateGiftCardCode(),
                                    RecipientName = giftCardRecipientName,
                                    RecipientEmail = giftCardRecipientEmail,
                                    SenderName = giftCardSenderName,
                                    SenderEmail = giftCardSenderEmail,
                                    Message = giftCardMessage,
                                    IsRecipientNotified = false,
                                    CreatedOnUtc = DateTime.UtcNow
                                });
                            }
                        }

                        //inventory
                        _productService.AdjustInventory(sc.Product, -sc.Quantity, sc.AttributesXml,
                            string.Format(_localizationService.GetResource("Admin.StockQuantityHistory.Messages.PlaceOrder"), order.Id));
                    }

                    //clear shopping cart
                    details.Cart.ToList().ForEach(sci => _shoppingCartService.DeleteShoppingCartItem(sci, false));

                    //discount usage history
                    foreach (var discount in details.AppliedDiscounts)
                    {
                        var d = _discountService.GetDiscountById(discount.Id);
                        if (d != null)
                        {
                            _discountService.InsertDiscountUsageHistory(new DiscountUsageHistory
                            {
                                Discount = d,
                                Order = order,
                                CreatedOnUtc = DateTime.UtcNow
                            });
                        }
                    }

                    //gift card usage history
                    if (details.AppliedGiftCards != null)
                        foreach (var agc in details.AppliedGiftCards)
                        {
                            agc.GiftCard.GiftCardUsageHistory.Add(new GiftCardUsageHistory
                            {
                                GiftCard = agc.GiftCard,
                                UsedWithOrder = order,
                                UsedValue = agc.AmountCanBeUsed,
                                CreatedOnUtc = DateTime.UtcNow
                            });
                            _giftCardService.UpdateGiftCard(agc.GiftCard);
                        }

                    //recurring orders
                    if (details.IsRecurringShoppingCart)
                    {
                        //create recurring payment (the first payment)
                        var rp = new RecurringPayment
                        {
                            CycleLength = processPaymentRequest.RecurringCycleLength,
                            CyclePeriod = processPaymentRequest.RecurringCyclePeriod,
                            TotalCycles = processPaymentRequest.RecurringTotalCycles,
                            StartDateUtc = DateTime.UtcNow,
                            IsActive = true,
                            CreatedOnUtc = DateTime.UtcNow,
                            InitialOrder = order,
                        };
                        _orderService.InsertRecurringPayment(rp);

                        switch (_paymentService.GetRecurringPaymentType(processPaymentRequest.PaymentMethodSystemName))
                        {
                            case RecurringPaymentType.NotSupported:
                                //not supported
                                break;
                            case RecurringPaymentType.Manual:
                                rp.RecurringPaymentHistory.Add(new RecurringPaymentHistory
                                {
                                    RecurringPayment = rp,
                                    CreatedOnUtc = DateTime.UtcNow,
                                    OrderId = order.Id,
                                });
                                _orderService.UpdateRecurringPayment(rp);
                                break;
                            case RecurringPaymentType.Automatic:
                                //will be created later (process is automated)
                                break;
                            default:
                                break;
                        }
                    }

                    #endregion

                    //notifications
                    SendNotificationsAndSaveNotes(order);

                    //reset checkout data
                    _customerService.ResetCheckoutData(details.Customer, processPaymentRequest.StoreId, clearCouponCodes: true, clearCheckoutAttributes: true);
                    _customerActivityService.InsertActivity("PublicStore.PlaceOrder", _localizationService.GetResource("ActivityLog.PublicStore.PlaceOrder"), order.Id);

                    //check order status
                    CheckOrderStatus(order);

                    //raise event       
                    _eventPublisher.Publish(new OrderPlacedEvent(order));

                    if (order.PaymentStatus == PaymentStatus.Paid)
                        ProcessOrderPaid(order);
                }
                else
                    foreach (var paymentError in processPaymentResult.Errors)
                        result.AddError(string.Format(_localizationService.GetResource("Checkout.PaymentError"), paymentError));
            }
            catch (Exception exc)
            {
                _logger.Error(exc.Message, exc);
                result.AddError(exc.Message);
            }

            #region Process errors

            if (!result.Success)
            {
                //log errors
                var logError = result.Errors.Aggregate("Error while placing order. ",
                    (current, next) => $"{current}Error {result.Errors.IndexOf(next) + 1}: {next}. ");
                var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
                _logger.Error(logError, customer: customer);
            }

            #endregion

            return result;
        }

        #endregion
    }
}