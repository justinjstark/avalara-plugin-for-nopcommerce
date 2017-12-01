using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Tax.Avalara.Factories;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Orders;
using Nop.Web.Factories;

namespace Nop.Plugin.Tax.Avalara.Infrastructure
{
    /// <summary>
    /// Dependency registrar of the Avalara tax provider services
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            //register overridden services and factories
            builder.RegisterType<OverriddenOrderProcessingService>().As<IOrderProcessingService>().InstancePerLifetimeScope();
            builder.RegisterType<OverriddenShoppingCartModelFactory>().As<IShoppingCartModelFactory>().InstancePerLifetimeScope();

            //register custom service
            builder.RegisterType<AvalaraTaxManager>().AsSelf().InstancePerLifetimeScope();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 4; }
        }
    }
}