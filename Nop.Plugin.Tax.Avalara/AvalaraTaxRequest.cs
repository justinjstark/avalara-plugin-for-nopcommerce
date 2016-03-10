using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nop.Plugin.Tax.Avalara
{
    public class AvalaraTaxRequest
    {
        public string DocDate { get; set; }

        public string CustomerCode { get; set; }

        public Address[] Addresses { get; set; }

        public Line[] Lines { get; set; }

        public string Client { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DocType DocType { get; set; }

        public string CompanyCode { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DetailLevel DetailLevel { get; set; }

        public string BusinessIdentificationNo { get; set; }

        public string CurrencyCode { get; set; }
    }

    public class Address
    {
        public string AddressCode { get; set; }

        public string Line1 { get; set; }

        public string Line2 { get; set; }

        public string City { get; set; }

        public string Region { get; set; }

        public string PostalCode { get; set; }

        public string Country { get; set; }
    }

    public class Line
    {
        public string LineNo { get; set; }

        public string DestinationCode { get; set; }

        public string OriginCode { get; set; }

        public decimal Qty { get; set; }

        public decimal Amount { get; set; }
    }

    public class AvalaraTaxResult
    {
        public string DocCode { get; set; }

        public DateTime DocDate { get; set; }

        public DateTime TimeStamp { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal TotalDiscount { get; set; }

        public decimal TotalExemption { get; set; }

        public decimal TotalTaxable { get; set; }

        public decimal TotalTax { get; set; }

        public decimal TotalTaxCalculated { get; set; }

        public DateTime TaxDate { get; set; }

        public TaxLine[] TaxLines { get; set; }

        public TaxLine[] TaxSummary { get; set; }

        public TaxAddress[] TaxAddresses { get; set; }

        public SeverityLevel ResultCode { get; set; }

        public Message[] Messages { get; set; }
    }
  
    public class TaxLine
    {
        public string LineNo { get; set; }

        public string TaxCode { get; set; }

        public bool Taxability { get; set; }

        public decimal Taxable { get; set; }

        public decimal Rate { get; set; }

        public decimal Tax { get; set; }

        public decimal Discount { get; set; }

        public decimal TaxCalculated { get; set; }

        public decimal Exemption { get; set; }

        public TaxDetail[] TaxDetails { get; set; }
    }

    public class TaxDetail
    {
        public decimal Rate { get; set; }

        public decimal Tax { get; set; }

        public decimal Taxable { get; set; }

        public string Country { get; set; }

        public string Region { get; set; }

        public string JurisType { get; set; }

        public string JurisName { get; set; }

        public string TaxName { get; set; }
    }

    public class TaxAddress
    {
        public string Address { get; set; }

        public string AddressCode { get; set; }

        public string City { get; set; }

        public string Region { get; set; }

        public string Country { get; set; }

        public string PostalCode { get; set; }

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public string TaxRegionId { get; set; }

        public string JurisCode { get; set; }
    }

    public class Message
    {
        public string Summary { get; set; }

        public string Details { get; set; }

        public string RefersTo { get; set; }

        public SeverityLevel Severity { get; set; }

        public string Source { get; set; }
    }

    public enum DocType
    {
        SalesOrder,
        SalesInvoice,
        ReturnOrder,
        ReturnInvoice,
        PurchaseOrder,
        PurchaseInvoice,
        ReverseChargeOrder,
        ReverseChargeInvoice
    }

    public enum DetailLevel
    {
        Tax,
        Document,
        Line,
        Diagnostic
    }

    public enum SeverityLevel
    {
        Success,
        Warning,
        Error,
        Exception
    }    
}
