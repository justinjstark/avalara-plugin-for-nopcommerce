using System.Web.Mvc;
using Autofac;
using Autofac.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Orders;

namespace Nop.Plugin.Tax.Avalara.Infrastructure
{
    /// <summary>
    /// Dependency registrar of the Avalara tax provider
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
            //we cache tax rate between requests, so use static cache manager 
            builder.RegisterType<AvalaraTaxProvider>().WithParameter(ResolvedParameter.ForNamed<ICacheManager>("nop_cache_static"));

            //register custom services
            builder.RegisterType<AvalaraOrderProcessingService>().As<IOrderProcessingService>().InstancePerLifetimeScope();
            builder.RegisterType<AvalaraImportManager>().AsSelf().InstancePerLifetimeScope();

            //register custom filter provider
            builder.RegisterType<OverrideTaxCategoriesFilterAttribute>().As<IFilterProvider>();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 2; }
        }
    }
}
