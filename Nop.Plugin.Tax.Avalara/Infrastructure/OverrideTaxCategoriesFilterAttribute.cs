using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using Nop.Admin.Controllers;
using Nop.Admin.Models.Tax;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Plugin.Tax.Avalara.Controllers;
using Nop.Services.Tax;

namespace Nop.Plugin.Tax.Avalara.Infrastructure
{
    /// <summary>
    /// Represents filter that is used to override tax controller actions related with tax categories
    /// </summary>
    public class OverrideTaxCategoriesFilterAttribute : ActionFilterAttribute, IFilterProvider
    {
        /// <summary>
        /// Returns an enumerator that contains all IFilterProvider instances in the service locator
        /// </summary>
        /// <param name="controllerContext">The controller context</param>
        /// <param name="actionDescriptor">The action descriptor</param>
        /// <returns>Collection of the IFilterProvider instances</returns>
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            var filters = new List<Filter>();

            //add this filter to the admin tax controller actions
            if (controllerContext.Controller is TaxController)
                filters.Add(new Filter(this, FilterScope.Action, 0));

            return filters;
        }

        /// <summary>
        /// Called by the ASP.NET MVC framework before the action method executes
        /// </summary>
        /// <param name="filterContext">The filter context</param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null || filterContext.HttpContext.Request == null)
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            //ensure that Avalara tax rate provider is active
            var taxService = EngineContext.Current.Resolve<ITaxService>();
            var customer  = EngineContext.Current.Resolve<IWorkContext>().CurrentCustomer;
            var taxProvider = taxService.LoadActiveTaxProvider(customer) as AvalaraTaxProvider;
            if (taxProvider == null)
                return;

            //use actions of the overridden Avalara tax controller
            var taxController = EngineContext.Current.Resolve<TaxCategoriesController>();
            switch (filterContext.ActionDescriptor.ActionName)
            {
                case "Categories":
                    if (filterContext.HttpContext.Request.HttpMethod == WebRequestMethods.Http.Get)
                        filterContext.Result = taxController.Categories();
                    else
                    {
                        var command = (Web.Framework.Kendoui.DataSourceRequest)filterContext.ActionParameters["command"];
                        filterContext.Result = taxController.Categories(command);
                    }
                    break;
                case "CategoryAdd":
                    var modelCreate = (TaxCategoryModel)filterContext.ActionParameters["model"];
                    filterContext.Result = taxController.CategoryAdd(modelCreate);
                    break;
                case "CategoryUpdate":
                    var modelUpdate = (TaxCategoryModel)filterContext.ActionParameters["model"];
                    filterContext.Result = taxController.CategoryUpdate(modelUpdate);
                    break;
                case "CategoryDelete":
                    var id = (int)filterContext.ActionParameters["id"];
                    filterContext.Result = taxController.CategoryDelete(id);
                    break;
            }            
        }
    }
}
