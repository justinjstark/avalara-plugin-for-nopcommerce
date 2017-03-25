using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nop.Plugin.Tax.Avalara.Domain
{
    #region Requests

    /// <summary>
    /// Represents the tax request to Avalara service
    /// </summary>
    public class TaxRequest
    {
        /// <summary>
        /// Gets or sets the buyer’s VAT ID. Using this value will force VAT rules to be considered for the transaction.
        /// </summary>
        public string BusinessIdentificationNo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to put the document in a Committed status, 
        /// preventing further document status changes, except voiding with CancelTax.
        /// </summary>
        public bool Commit { get; set; }

        /// <summary>
        /// Gets or sets an identifier of software client generating the API call.
        /// </summary>
        public string Client { get; set; }

        /// <summary>
        /// Gets or sets the case-sensitive code that identifies the company in the AvaTax account in which the document should be posted. 
        /// </summary>
        public string CompanyCode { get; set; }

        /// <summary>
        /// Gets or sets the case-sensitive client application customer reference code.
        /// </summary>
        public string CustomerCode { get; set; }

        /// <summary>
        /// Gets or sets 3 character ISO 4217 compliant currency code. If unspecified, a default of USD will be used.
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the client application customer or usage type. 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CustomerUsageType CustomerUsageType { get; set; }

        /// <summary>
        /// Gets or sets the level of detail to return.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DetailLevel DetailLevel { get; set; }

        /// <summary>
        /// Gets or sets the discount amount to apply to the document.
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Gets or sets a value to keep the document unique.
        /// </summary>
        public string DocCode { get; set; }

        /// <summary>
        /// Gets or sets the date on the invoice, purchase order, etc. Format YYYY-MM-DD.
        /// </summary>
        public string DocDate { get; set; }

        /// <summary>
        /// Gets or sets the document type specifies the category of the document and affects how the document is treated after a tax calculation.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DocType DocType { get; set; }

        /// <summary>
        /// Gets or sets a string value will cause the sale to be exempt.
        /// </summary>
        public string ExemptionNo { get; set; }

        /// <summary>
        /// Gets or sets a value assigned by some state jurisdictions that identifies a particular store location.
        /// </summary>
        public string LocationCode { get; set; }

        /// <summary>
        /// Gets or sets the unique code / ID / number associated with the terminal processing a sale.
        /// </summary>
        public string PosLaneCode { get; set; }

        /// <summary>
        /// Gets or sets customer’s purchase order number.
        /// </summary>
        public string PurchaseOrderNo { get; set; }

        /// <summary>
        /// Gets or sets an additional information used for reporting.
        /// </summary>
        public string ReferenceCode { get; set; }

        /// <summary>
        /// Gets or sets an array of addresses
        /// </summary>
        public Address[] Addresses { get; set; }

        /// <summary>
        /// Gets or sets an array of item lines
        /// </summary>
        public Line[] Lines { get; set; }

    }

    /// <summary>
    /// Represents the cancel tax request to Avalara service
    /// </summary>
    public class CancelTaxRequest
    {
        /// <summary>
        /// Gets or sets the reason for cancelling the tax record. 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CancelReason CancelCode { get; set; }

        /// <summary>
        /// Gets or sets client application company reference code. 
        /// </summary>
        public string CompanyCode { get; set; }

        /// <summary>
        /// Gets or sets client application identifier describing this tax transaction
        /// </summary>
        public string DocCode { get; set; }

        /// <summary>
        /// Gets or sets a value describing what type of tax document is being cancelled. 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DocType DocType { get; set; }

        /// <summary>
        /// Gets or sets Avatax-assigned unique Document Id, can be used in place of DocCode, DocType, and CompanyCode.
        /// </summary>
        public string DocId { get; set; }
    }

    /// <summary>
    /// Represents an address in tax request
    /// </summary>
    public class Address
    {
        /// <summary>
        /// Gets or sets reference code uniquely identifying this address instance.
        /// </summary>
        public string AddressCode { get; set; }

        /// <summary>
        /// Gets or sets an address line 1.
        /// </summary>
        public string Line1 { get; set; }

        /// <summary>
        /// Gets or sets an address line 2.
        /// </summary>
        public string Line2 { get; set; }

        /// <summary>
        /// Gets or sets an address line 3.
        /// </summary>
        public string Line3 { get; set; }

        /// <summary>
        /// Gets or sets a city name.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets a state, province, or region name.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets a two-character ISO country code. 
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets a postal or ZIP code.
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets an AvaTax tax region identifier. If a non-zero value is used, other fields will be ignored. 
        /// </summary>
        public int TaxRegionId { get; set; }
    }

    /// <summary>
    /// Represents an item line in tax request
    /// </summary>
    public class Line
    {
        /// <summary>
        /// Gets or sets an identifier that uniquely identifies the line item row.
        /// </summary>
        public string LineNo { get; set; }

        /// <summary>
        /// Gets or sets the destination (ship-to) address code. DestinationCode references an address from the Addresses collection.
        /// </summary>
        public string DestinationCode { get; set; }

        /// <summary>
        /// Gets or sets the origination (ship-from) address code. OriginCode references an address from the Addresses collection.
        /// </summary>
        public string OriginCode { get; set; }

        /// <summary>
        /// Gets or sets an item identifier, SKU, or UPC.
        /// </summary>
        public string ItemCode { get; set; }

        /// <summary>
        /// Gets or sets a product taxability code of the line item. Can be an AvaTax system tax code, or a custom-defined tax code.
        /// </summary>
        public string TaxCode { get; set; }

        /// <summary>
        /// Gets or sets the client application customer or usage type. 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CustomerUsageType CustomerUsageType { get; set; }

        /// <summary>
        /// Gets or sets the buyer’s VAT ID. Using this value will force VAT rules to be considered for the transaction.
        /// </summary>
        public string BusinessIdentificationNo { get; set; }

        /// <summary>
        /// Gets or sets an item description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets an item quantity. The tax engine does NOT use this as a multiplier with price to get the Amount.
        /// </summary>
        public decimal Qty { get; set; }

        /// <summary>
        /// Gets or sets a total amount of item (extended amount, qty * unit price).
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets a string value will cause the sale to be exempt.
        /// </summary>
        [Obsolete]
        [JsonIgnore]
        public string ExemptionNo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the document level discount is applied to this line item.
        /// </summary>
        public bool Discounted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tax is already included, and sale amount and tax should be back-calculated from the provided Line.Amount.
        /// </summary>
        public bool TaxIncluded { get; set; }

        /// <summary>
        /// Gets or sets a value stored on a line item. Does not affect tax calclulation.
        /// </summary>
        public string Ref1 { get; set; }

        /// <summary>
        /// Gets or sets a value stored on a line item. Does not affect tax calclulation.
        /// </summary>
        public string Ref2 { get; set; }
    }

    #endregion

    #region Responses

    /// <summary>
    /// Represents tax response
    /// </summary>
    public class TaxResponse
    {
        /// <summary>
        /// Gets or sets a value to keep the document unique.
        /// </summary>
        public string DocCode { get; set; }

        /// <summary>
        /// Gets or sets date of invoice, sales order, purchase order, etc.
        /// </summary>
        public DateTime DocDate { get; set; }

        /// <summary>
        /// Gets or sets server timestamp of request.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets sum of all line Amount values.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets sum of all TaxLine discount amounts.
        /// </summary>
        public decimal TotalDiscount { get; set; }

        /// <summary>
        /// Gets or sets total exemption amount.
        /// </summary>
        public decimal TotalExemption { get; set; }

        /// <summary>
        /// Gets or sets total taxable amount.
        /// </summary>
        public decimal TotalTaxable { get; set; }

        /// <summary>
        /// Gets or sets sum of all TaxLine tax amounts.
        /// </summary>
        public decimal TotalTax { get; set; }

        /// <summary>
        /// Gets or sets indicates the total tax calculated by AvaTax. This is usually the same as the TotalTax, except when a tax override amount is specified. 
        /// </summary>
        public decimal TotalTaxCalculated { get; set; }

        /// <summary>
        /// Gets or sets date used to assess tax rates and jurisdictions.
        /// </summary>
        public DateTime TaxDate { get; set; }

        /// <summary>
        /// Gets or sets tax lines
        /// </summary>
        public TaxLine[] TaxLines { get; set; }

        /// <summary>
        /// Gets or sets tax summary as tax lines
        /// </summary>
        public TaxLine[] TaxSummary { get; set; }

        /// <summary>
        /// Gets or sets tax addresses
        /// </summary>
        public TaxAddress[] TaxAddresses { get; set; }

        /// <summary>
        /// Gets or sets severity of message.
        /// </summary>
        public SeverityLevel ResultCode { get; set; }

        /// <summary>
        /// Gets or sets messages
        /// </summary>
        public Message[] Messages { get; set; }
    }

    /// <summary>
    /// Represents cancel tax response
    /// </summary>
    public class CancelTaxResponse
    {
        /// <summary>
        /// Gets or sets the unique numeric identifier of the API operation assigned by the AvaTax service.
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets the unique numeric identifier (Document ID) assigned to the tax document in question by the AvaTax Service.
        /// </summary>
        public string DocId { get; set; }

        /// <summary>
        /// Gets or sets severity of message.
        /// </summary>
        public SeverityLevel ResultCode { get; set; }

        /// <summary>
        /// Gets or sets messages
        /// </summary>
        public Message[] Messages { get; set; }
    }

    /// <summary>
    /// Represents a response to validating address request
    /// </summary>
    public class ValidateAddressResponse
    {
        /// <summary>
        /// Gets or sets validated address
        /// </summary>
        [JsonProperty(PropertyName = "Address")]
        public ValidatedAddress ValidatedAddress { get; set; }

        /// <summary>
        /// Gets or sets severity of message.
        /// </summary>
        public SeverityLevel ResultCode { get; set; }

        /// <summary>
        /// Gets or sets messages
        /// </summary>
        public Message[] Messages { get; set; }
    }

    /// <summary>
    /// Represents tax line
    /// </summary>
    public class TaxLine
    {
        /// <summary>
        /// Gets or sets line item identifier.
        /// </summary>
        public string LineNo { get; set; }

        /// <summary>
        /// Gets or sets the tax code used in calculating tax.
        /// </summary>
        public string TaxCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether item was taxable.
        /// </summary>
        public bool Taxability { get; set; }

        /// <summary>
        /// Gets or sets the amount that is taxable.
        /// </summary>
        public decimal Taxable { get; set; }

        /// <summary>
        /// Gets or sets effective tax rate.
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// Gets or sets tax amount.
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Gets or sets discount amount.
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Gets or sets amount of tax calculated.
        /// </summary>
        public decimal TaxCalculated { get; set; }

        /// <summary>
        /// Gets or sets exempt amount.
        /// </summary>
        public decimal Exemption { get; set; }

        /// <summary>
        /// Gets or sets the boundary level used to calculate tax: determined by the quality of provided addresses.
        /// </summary>
        public string BoundaryLevel { get; set; }

        /// <summary>
        /// Gets or sets tax details
        /// </summary>
        public TaxDetail[] TaxDetails { get; set; }
    }

    /// <summary>
    /// Represents tax detail
    /// </summary>
    public class TaxDetail
    {
        /// <summary>
        /// Gets or sets effective tax rate for tax jurisdiction.
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// Gets or sets tax amount
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Gets or sets two character ISO country code.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets region of tax jurisdiction.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets jurisdiction type
        /// </summary>
        public string JurisType { get; set; }

        /// <summary>
        /// Gets or sets name of a tax jurisdiction
        /// </summary>
        public string JurisName { get; set; }

        /// <summary>
        /// Gets or sets state assigned code identifying the jurisdiction.
        /// </summary>
        public string JurisCode { get; set; }

        /// <summary>
        /// Gets or sets tax name
        /// </summary>
        public string TaxName { get; set; }
    }

    /// <summary>
    /// Represents tax address
    /// </summary>
    public class TaxAddress
    {
        /// <summary>
        /// Gets or sets a canonical street address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets a reference code uniquely identifying this address instance. AddressCode will always correspond to an address code supplied to in the address collection provided in the request.
        /// </summary>
        public string AddressCode { get; set; }

        /// <summary>
        /// Gets or sets city name
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets state or region name
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets country code, as ISO 3166-1 country code (e.g. "US")
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets postal code
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets geographic latitude.
        /// </summary>
        public decimal Latitude { get; set; }

        /// <summary>
        /// Gets or sets geographic longitude.
        /// </summary>
        public decimal Longitude { get; set; }

        /// <summary>
        /// Gets or sets AvaTax tax region identifier.
        /// </summary>
        public string TaxRegionId { get; set; }

        /// <summary>
        /// Gets or sets tax jurisdiction code.
        /// </summary>
        public string JurisCode { get; set; }
    }

    /// <summary>
    /// Represents a response message
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the message summary in short form.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets description of the error or warning.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Gets or sets the data used during the request that caused the message to be generated.
        /// </summary>
        public string RefersTo { get; set; }

        /// <summary>
        /// Gets or sets the severity of the message.
        /// </summary>
        public SeverityLevel Severity { get; set; }

        /// <summary>
        /// Gets or sets the internal location that generated the message.
        /// </summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// Represents validated address
    /// </summary>
    public class ValidatedAddress
    {
        /// <summary>
        /// Gets or sets an address line 1.
        /// </summary>
        public string Line1 { get; set; }

        /// <summary>
        /// Gets or sets an address line 2.
        /// </summary>
        public string Line2 { get; set; }

        /// <summary>
        /// Gets or sets an address line 3.
        /// </summary>
        public string Line3 { get; set; }

        /// <summary>
        /// Gets or sets a city name.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets a state, province, or region name.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets a two-character ISO country code. 
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets a postal or ZIP code.
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public AddressType AddressType { get; set; }

        /// <summary>
        /// Gets or sets a unique 10-digit code representing each geographic combination of state, county, and city. The code is made up of the Federal Information Processing Code (FIPS) that uniquely identifies each state, county, and city in the U.S. Returned for US addresses only. Digits represent jurisdiction codes: * 1-2 State code * 3-5 County code * 6-10 City code
        /// </summary>
        public string FipsCode { get; set; }

        /// <summary>
        /// Gets or sets a four-character string representing a US postal carrier route. The first character of this property, the term, is always alphabetic, and the last three numeric. For example, “R001” or “C027” would be typical carrier routes. 
        /// </summary>
        public string CarrierRoute { get; set; }

        /// <summary>
        /// Gets or sets a 12-digit barcode containing the ZIP Code, ZIP+4 Code, and the delivery point code, used by the USPS to direct mail. Returned for US addresses only digits represent delivery information: * 1-5 ZIP code * 6-9 Plus4 code * 10-11 Delivery point * 12 Check digit
        /// </summary>
        public string PostNet { get; set; }
    }

    #endregion

    #region Enumerations

    /// <summary>
    /// Enumeration of the document types
    /// </summary>
    public enum DocType
    {
        /// <summary>
        /// Temporary document type and is not saved in tax history
        /// </summary>
        SalesOrder,

        /// <summary>
        /// The document is a permanent invoice; document and tax calculation results are saved in the tax history
        /// </summary>
        SalesInvoice,

        /// <summary>
        /// Temporary document type and is not saved in tax history
        /// </summary>
        PurchaseOrder,

        /// <summary>
        /// The document is a permanent invoice; document and tax calculation results are saved in the tax history
        /// </summary>
        PurchaseInvoice,

        /// <summary>
        /// Temporary document type and is not saved in tax history
        /// </summary>
        ReturnOrder,

        /// <summary>
        /// The document is a permanent sales return invoice; document and tax calculation results are saved in the tax history
        /// </summary>
        ReturnInvoice
    }

    /// <summary>
    /// Enumeration of returned detail levels
    /// </summary>
    public enum DetailLevel
    {
        /// <summary>
        /// Summarizes document and jurisdiction detail with no line breakout
        /// </summary>
        Summary,

        /// <summary>
        /// Only document detail
        /// </summary>
        Document,

        /// <summary>
        /// Document and line detail 
        /// </summary>
        Line,

        /// <summary>
        /// Document, line and jurisdiction detail
        /// </summary>
        Tax
    }

    /// <summary>
    /// Enumeration of severity levels of response message
    /// </summary>
    public enum SeverityLevel
    {
        /// <summary>
        /// Success
        /// </summary>
        Success,

        /// <summary>
        /// Warning
        /// </summary>
        Warning,

        /// <summary>
        /// Error
        /// </summary>
        Error,

        /// <summary>
        /// Exception
        /// </summary>
        Exception
    }

    /// <summary>
    /// Enumeration of customer usage types (used for tax exempt)
    /// </summary>
    public enum CustomerUsageType
    {
        /// <summary>
        /// Non exempt
        /// </summary>
        X,

        /// <summary>
        /// Federal government (United States)
        /// </summary>
        A,

        /// <summary>
        /// State government (United States)
        /// </summary>
        B,

        /// <summary>
        /// Tribe / Status Indian / Indian Band (both)
        /// </summary>
        C,

        /// <summary>
        /// Foreign diplomat (both)
        /// </summary>
        D,

        /// <summary>
        /// Charitable or benevolent org (both)
        /// </summary>
        E,

        /// <summary>
        /// Religious or educational org (both)
        /// </summary>
        F,

        /// <summary>
        /// Resale (both)
        /// </summary>
        G,

        /// <summary>
        /// Commercial agricultural production (both)
        /// </summary>
        H,

        /// <summary>
        /// Industrial production / manufacturer (both)
        /// </summary>
        I,

        /// <summary>
        /// Direct pay permit (United States)
        /// </summary>
        J,

        /// <summary>
        /// Direct mail (United States)
        /// </summary>
        K,

        /// <summary>
        /// Custom exempt (by nopCommerce)
        /// </summary>
        L,

        /// <summary>
        /// Local government (United States)
        /// </summary>
        N,

        /// <summary>
        /// Commercial aquaculture (Canada)
        /// </summary>
        P,

        /// <summary>
        /// Commercial Fishery (Canada)
        /// </summary>
        Q,

        /// <summary>
        /// Non-resident (Canada)
        /// </summary>
        R
    }

    /// <summary>
    /// Enumeration of cancel reasons
    /// </summary>
    public enum CancelReason
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        Unspecified,

        /// <summary>
        /// Failed
        /// </summary>
        PostFailed,

        /// <summary>
        /// Order deleted
        /// </summary>
        DocDeleted,

        /// <summary>
        /// Order voided
        /// </summary>
        DocVoided,

        /// <summary>
        /// Adjustment cancelled
        /// </summary>
        AdjustmentCancelled
    }

    /// <summary>
    /// Enumeration of address types
    /// </summary>
    public enum AddressType
    {
        /// <summary>
        /// Firm or company address
        /// </summary>
        F,

        /// <summary>
        /// General Delivery address
        /// </summary>
        G,

        /// <summary>
        /// High-rise or business complex
        /// </summary>
        H,

        /// <summary>
        /// PO Box address
        /// </summary>
        P,

        /// <summary>
        /// Rural route address
        /// </summary>
        R,

        /// <summary>
        /// Street or residential address
        /// </summary>
        S
    }

    #endregion
}
