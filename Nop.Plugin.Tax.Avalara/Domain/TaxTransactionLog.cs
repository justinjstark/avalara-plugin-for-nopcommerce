using System;
using Nop.Core;

namespace Nop.Plugin.Tax.Avalara.Domain
{
    /// <summary>
    /// Represents a tax transaction log record
    /// </summary>
    public partial class TaxTransactionLog : BaseEntity
    {
        /// <summary>
        /// Gets or sets the message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the date and time of creation
        /// </summary>
        public DateTime CreatedDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the log type identifier
        /// </summary>
        public int LogTypeId { get; set; }

        /// <summary>
        /// Gets or sets the log type
        /// </summary>
        public LogType LogType
        {
            get { return (LogType)LogTypeId; }
            set { LogTypeId = (int)value; }
        }
    }
}