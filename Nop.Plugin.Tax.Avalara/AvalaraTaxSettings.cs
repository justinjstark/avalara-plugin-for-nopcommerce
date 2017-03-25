using Nop.Core.Configuration;

namespace Nop.Plugin.Tax.Avalara
{
    /// <summary>
    /// Represents settings for the Avalara tax provider 
    /// </summary>
    public class AvalaraTaxSettings : ISettings
    {
        /// <summary>
        /// Gets or sets Avalara account ID
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Gets or sets Avalara account license key
        /// </summary>
        public string LicenseKey { get; set; }

        /// <summary>
        /// Gets or sets company code
        /// </summary>
        public string CompanyCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool IsSandboxEnvironment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to commit tax transactions (recorded in the history on your Avalara account)
        /// </summary>
        public bool CommitTransactions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to validate addresses before tax requesting (only for US or Canadian address)
        /// </summary>
        public bool ValidateAddresses { get; set; }
    }
}