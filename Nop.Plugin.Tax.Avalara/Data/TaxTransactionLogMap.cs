using Nop.Data.Mapping;
using Nop.Plugin.Tax.Avalara.Domain;

namespace Nop.Plugin.Tax.Avalara.Data
{
    /// <summary>
    /// Represents the tax transaction log mapping class
    /// </summary>
    public partial class TaxTransactionLogMap : NopEntityTypeConfiguration<TaxTransactionLog>
    {
        public TaxTransactionLogMap()
        {
            this.ToTable(nameof(TaxTransactionLog));
            this.HasKey(logItem => logItem.Id);
            this.Property(logItem => logItem.Message).IsRequired();
            this.Ignore(logItem => logItem.LogType);
        }
    }
}