using Nop.Core.Infrastructure;
using Nop.Services.Tax;

namespace Nop.Plugin.Tax.Avalara
{
    /// <summary>
    /// Represents constants of the Avalara tax provider
    /// </summary>
    public class AvalaraTaxDefaults
    {
        /// <summary>
        /// Avalara tax provider system name
        /// </summary>
        public static string SystemName => "Tax.Avalara";

        /// <summary>
        /// Avalara tax provider connector name
        /// </summary>
        public static string ApplicationName => "nopCommerce-AvalaraTaxRateProvider";

        /// <summary>
        /// Avalara tax provider version
        /// </summary>
        public static string ApplicationVersion => EngineContext.Current.Resolve<ITaxService>()?
            .LoadTaxProviderBySystemName(SystemName)?.PluginDescriptor.Version ?? "2.00";

        /// <summary>
        /// Name of the generic attribute that is used to store Avalara system tax code description
        /// </summary>
        public static string TaxCodeDescriptionAttribute => "AvalaraTaxCodeDescription";

        /// <summary>
        /// Name of the generic attribute that is used to store a tax code type
        /// </summary>
        public static string TaxCodeTypeAttribute => "AvalaraTaxCodeType";

        /// <summary>
        /// Name of the generic attribute that is used to store an entity use code (customer usage type)
        /// </summary>
        public static string EntityUseCodeAttribute => "AvalaraEntityUseCode";

        /// <summary>
        /// Name of a process payment request custom value to store tax total received from the Avalara
        /// </summary>
        public static string TaxTotalCustomValue => "AvalaraTaxTotal";

        /// <summary>
        /// Name of the field to specify the tax origin address type
        /// </summary>
        public static string TaxOriginField => "AvalaraTaxOriginAddressType";

        /// <summary>
        /// Key for caching tax rate for the specified address
        /// </summary>
        /// <remarks>
        /// {0} - Address
        /// {1} - City
        /// {2} - State or province identifier
        /// {3} - Country identifier
        /// {4} - Zip postal code
        /// </remarks>
        public static string TaxRateCacheKey => "Nop.avalara.taxrate.address-{0}-{1}-{2}-{3}-{4}";

        /// <summary>
        /// Key for caching Avalara tax code types
        /// </summary>
        public static string TaxCodeTypesCacheKey => "Nop.avalara.taxcodetypes";

        /// <summary>
        /// Key for caching Avalara system entity use codes
        /// </summary>
        public static string EntityUseCodesCacheKey => "Nop.avalara.entityusecodes";

        /// <summary>
        /// Name of the view component to display entity use code field
        /// </summary>
        public const string EntityUseCodeViewComponentName = "AvalaraEntityUseCode";

        /// <summary>
        /// Name of the view component to display tax origin address type field
        /// </summary>
        public const string TaxOriginViewComponentName = "AvalaraTaxOrigin";

        /// <summary>
        /// Name of the view component to display export items button
        /// </summary>
        public const string ExportItemsViewComponentName = "AvalaraExportItems";

        /// <summary>
        /// Name of the view component to display entity use code
        /// </summary>
        public const string TaxCodesViewComponentName = "AvalaraTaxCodes";

        /// <summary>
        /// Name of the view component to validate entered address
        /// </summary>
        public const string AddressValidationViewComponentName = "AvalaraAddressValidation";

        /// <summary>
        /// Customer details widget zone
        /// </summary>
        public const string CustomerDetailsWidgetZone = "admin_customer_details_info_top";

        /// <summary>
        /// Customer role details widget zone
        /// </summary>
        public const string CustomerRoleDetailsWidgetZone = "admin_customer_role_details_top";

        /// <summary>
        /// Product details widget zone
        /// </summary>
        public const string ProductDetailsWidgetZone = "admin_product_details_info_column_left_top";

        /// <summary>
        /// Checkout attribute details widget zone
        /// </summary>
        public const string CheckoutAttributeDetailsWidgetZone = "admin_checkout_attribute_details_info_top";

        /// <summary>
        /// Tax settings widget zone
        /// </summary>
        public const string TaxSettingsWidgetZone = "admin_tax_settings_top";

        /// <summary>
        /// Product list buttons widget zone
        /// </summary>
        public const string ProductListButtonsWidgetZone = "admin_product_list_buttons";

        /// <summary>
        /// Tax categories buttons widget zone
        /// </summary>
        public const string TaxCategoriesButtonsWidgetZone = "admin_tax_category_list_buttons";

        /// <summary>
        /// Checkout confirmation page widget zone
        /// </summary>
        public const string CheckoutConfirmPageWidgetZone = "checkout_confirm_top";

        /// <summary>
        /// One page checkout confirmation page widget zone
        /// </summary>
        public const string OnePageCheckoutConfirmPageWidgetZone = "op_checkout_confirm_top";
    }
}