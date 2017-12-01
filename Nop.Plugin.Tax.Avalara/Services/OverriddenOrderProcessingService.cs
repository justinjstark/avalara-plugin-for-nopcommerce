using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
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
    /// <summary>
    /// Represents overridden order processing service
    /// </summary>
    public class OverriddenOrderProcessingService : OrderProcessingService
    {
        #region Fields

        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public OverriddenOrderProcessingService(IOrderService orderService,
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
            this._eventPublisher = eventPublisher;
            this._localizationService = localizationService;
            this._logger = logger;
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
            if (processPaymentRequest == null)
                throw new ArgumentNullException(nameof(processPaymentRequest));

            var result = new PlaceOrderResult();
            try
            {
                if (processPaymentRequest.OrderGuid == Guid.Empty)
                    processPaymentRequest.OrderGuid = Guid.NewGuid();

                //prepare order details
                var details = PreparePlaceOrderDetails(processPaymentRequest);

                //get previously saved tax total received from the Avalara tax service
                if (processPaymentRequest.CustomValues.TryGetValue(AvalaraTaxDefaults.TaxTotalCustomValue, out object taxTotalObject))
                {
                    if (decimal.TryParse(taxTotalObject.ToString(), out decimal taxTotal))
                    {
                        details.OrderTotal += (taxTotal - details.OrderTaxTotal);
                        processPaymentRequest.OrderTotal = details.OrderTotal;
                        details.OrderTaxTotal = taxTotal;
                    }

                    //delete custom value
                    processPaymentRequest.CustomValues.Remove(AvalaraTaxDefaults.TaxTotalCustomValue);
                }

                var processPaymentResult = GetProcessPaymentResult(processPaymentRequest, details);

                if (processPaymentResult == null)
                    throw new NopException("processPaymentResult is not available");

                if (processPaymentResult.Success)
                {
                    var order = SaveOrderDetails(processPaymentRequest, processPaymentResult, details);
                    result.PlacedOrder = order;

                    //move shopping cart items to order items
                    MoveShoppingCartItemsToOrderItems(details, order);

                    //discount usage history
                    SaveDiscountUsageHistory(details, order);

                    //gift card usage history
                    SaveGiftCardUsageHistory(details, order);

                    //recurring orders
                    if (details.IsRecurringShoppingCart)
                    {
                        CreateFirstRecurringPayment(processPaymentRequest, order);
                    }

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

            if (result.Success)
                return result;

            //log errors
            var logError = result.Errors.Aggregate("Error while placing order. ",
                (current, next) => $"{current}Error {result.Errors.IndexOf(next) + 1}: {next}. ");
            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            _logger.Error(logError, customer: customer);

            return result;
        }

        /// <summary>
        /// Voids order (from admin panel)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Voided order</returns>
        public override IList<string> Void(Order order)
        {
            var errors = base.Void(order);

            //raise event
            if (!errors?.Any() ?? true)
                _eventPublisher.Publish(new OrderVoidedEvent(order));

            return errors;
        }

        /// <summary>
        /// Voids order (offline)
        /// </summary>
        /// <param name="order">Order</param>
        public override void VoidOffline(Order order)
        {
            base.VoidOffline(order);

            //raise event       
            _eventPublisher.Publish(new OrderVoidedEvent(order));
        }

        #endregion
    }
}