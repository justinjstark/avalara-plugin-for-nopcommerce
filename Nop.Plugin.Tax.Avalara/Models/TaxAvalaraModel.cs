using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;

namespace Nop.Plugin.Tax.Avalara.Models
{
    public class TaxAvalaraModel
    {
        public TaxAvalaraModel()
        {
            TestAddress = new TaxAvalaraAddressModel();
        }

        [NopResourceDisplayName("Plugins.Tax.Avalara.AccountId")]
        public string AccountId { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.LicenseKey")]
        public string LicenseKey { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.CompanyCode")]
        public string CompanyCode { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.SaveRequests")]
        public bool SaveRequests { get; set; }

        [NopResourceDisplayName("Plugins.Tax.Avalara.SandboxEnvironment")]
        public bool SandboxEnvironment { get; set; }

        public TaxAvalaraAddressModel TestAddress { get; set; }

        public string TestingResult { get; set; }
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
    }
}