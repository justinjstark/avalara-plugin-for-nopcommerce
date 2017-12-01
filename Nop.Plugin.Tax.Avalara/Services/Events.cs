using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Tax.Avalara.Services
{
    /// <summary>
    /// Order voided event (currently missed in nopCommerce)
    /// </summary>
    public class OrderVoidedEvent
    {
        #region Ctor

        public OrderVoidedEvent(Order order)
        {
            this.Order = order;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Order
        /// </summary>
        public Order Order { get; }

        #endregion
    }
}