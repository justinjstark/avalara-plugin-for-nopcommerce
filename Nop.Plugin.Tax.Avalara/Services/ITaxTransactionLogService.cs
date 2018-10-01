using System;
using Nop.Core;
using Nop.Plugin.Tax.Avalara.Domain;

namespace Nop.Plugin.Tax.Avalara.Services
{
    /// <summary>
    /// Represents the tax transaction log service
    /// </summary>
    public interface ITaxTransactionLogService
    {
        /// <summary>
        /// Get tax transaction log
        /// </summary>
        /// <param name="customerId">Customer identifier; pass null to load all records</param>
        /// <param name="createdFromUtc">Log item creation from; pass null to load all records</param>
        /// <param name="createdToUtc">Log item creation to; pass null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged list of tax transaction log items</returns>
        IPagedList<TaxTransactionLog> GetTaxTransactionLog(int? customerId = null,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Get a log item by the identifier
        /// </summary>
        /// <param name="logItemId">Log item identifier</param>
        /// <returns>Log item</returns>
        TaxTransactionLog GetTaxTransactionLogById(int logItemId);

        /// <summary>
        /// Insert the log item
        /// </summary>
        /// <param name="logItem">Log item</param>
        void InsertTaxTransactionLog(TaxTransactionLog logItem);

        /// <summary>
        /// Update the log item
        /// </summary>
        /// <param name="logItem">Log item</param>
        void UpdateTaxTransactionLog(TaxTransactionLog logItem);

        /// <summary>
        /// Delete the log item
        /// </summary>
        /// <param name="logItem">Log item</param>
        void DeleteTaxTransactionLog(TaxTransactionLog logItem);

        /// <summary>
        /// Clear tax transaction log
        /// </summary>
        void ClearTaxTransactionLog();
    }
}