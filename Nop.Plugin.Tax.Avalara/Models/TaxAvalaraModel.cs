using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Tax.Avalara.Models
{
    public class TaxAvalaraModel
    {
        public TaxAvalaraModel()
        {
            TestAddress = new TaxAvalaraAddressModel();
        }

        public bool IsConfigured { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Fields.AccountId")]
        public string AccountId { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Fields.LicenseKey")]
        public string LicenseKey { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Fields.CompanyCode")]
        public string CompanyCode { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Fields.IsSandboxEnvironment")]
        public bool IsSandboxEnvironment { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Fields.CommitTransactions")]
        public bool CommitTransactions { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.Fields.ValidateAddresses")]
        public bool ValidateAddresses { get; set; }

        public TaxAvalaraAddressModel TestAddress { get; set; }

        public string TestTaxResult { get; set; }
    }

    public class TaxAvalaraAddressModel
    {
        public TaxAvalaraAddressModel()
        {
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.Address.Fields.Country")]
        public int CountryId { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.StateProvince")]
        public int RegionId { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.City")]
        public string City { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.Address1")]
        public string Address { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.ZipPostalCode")]
        public string ZipPostalCode { get; set; }
    }
}