using Nop.Core.Configuration;

namespace Nop.Plugin.Tax.Avalara
{
    public class AvalaraTaxSettings : ISettings
    {
        public string AccountId { get; set; }

        public string LicenseKey { get; set; }

        public string CompanyCode { get; set; }

        public bool SandboxEnvironment { get; set; }

        public bool SaveRequests { get; set; }
    }
}