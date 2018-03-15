
namespace Nop.Plugin.Tax.Avalara.Domain
{
    /// <summary>
    /// Represents a tax transaction log type
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Error
        /// </summary>
        Error = 1,

        /// <summary>
        /// Create transaction request
        /// </summary>
        Create = 2,

        /// <summary>
        /// Create transaction response
        /// </summary>
        CreateResponse = 3,

        /// <summary>
        /// Void transaction request
        /// </summary>
        Void = 4,

        /// <summary>
        /// Void transaction response
        /// </summary>
        VoidResponse = 5,

        /// <summary>
        /// Refund transaction request
        /// </summary>
        Refund = 6,

        /// <summary>
        /// Refund transaction response
        /// </summary>
        RefundResponse = 7
    }
}