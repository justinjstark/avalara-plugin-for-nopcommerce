using Nop.Core;

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
        public static string ApplicationName => "nopCommerce-AvalaraTaxRateProvider|a0o33000004BoPM";

        /// <summary>
        /// Avalara tax provider version (used a nopCommerce version here)
        /// </summary>
        public static string ApplicationVersion => $"v{NopVersion.CurrentVersion}";

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
        /// Name of a session custom value to store tax details received from the Avalara
        /// </summary>
        public static string TaxDetailsSessionValue => "AvalaraTaxDetails";

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
        /// Custom data objects context name
        /// </summary>
        public const string ObjectContextName = "nop_object_context_tax_avalara";
    }
}