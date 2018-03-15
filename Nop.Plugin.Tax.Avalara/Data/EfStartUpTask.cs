using System.Data.Entity;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.Tax.Avalara.Data
{
    /// <summary>
    /// Represents startup task that sets database initializer to null (for SQL Server Compact)
    /// </summary>
    public class EfStartUpTask : IStartupTask
    {
        /// <summary>
        /// Execute a task
        /// </summary>
        public void Execute()
        {
            //It's required to set initializer to null (for SQL Server Compact).
            //otherwise, you'll get something like "The model backing the 'your context name' context has changed since the database was created. Consider using Code First Migrations to update the database"
            Database.SetInitializer<TaxTransactionLogObjectContext>(null);
        }

        /// <summary>
        /// Gets order of this startup task implementation
        /// </summary>
        public int Order => 0;
    }
}