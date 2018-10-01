using System;
using System.Linq;
using Nop.Services.Cms;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Cms;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Tax.Avalara.Factories
{
    /// <summary>
    /// Represents the widget model factory implementation
    /// </summary>
    public partial class OverriddenWidgetModelFactory : WidgetModelFactory
    {
        #region Fields

        private readonly IWidgetService _widgetService;

        #endregion

        #region Ctor

        public OverriddenWidgetModelFactory(IWidgetService widgetService) : base(widgetService)
        {
            this._widgetService = widgetService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare paged widget list model
        /// </summary>
        /// <param name="searchModel">Widget search model</param>
        /// <returns>Widget list model</returns>
        public override WidgetListModel PrepareWidgetListModel(WidgetSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //exclude Avalara tax provider from the widget list
            var widgets = _widgetService.LoadAllWidgets()
                .Where(widget => !widget.PluginDescriptor.SystemName.Equals(AvalaraTaxDefaults.SystemName));

            //prepare grid model
            var model = new WidgetListModel
            {
                Data = widgets.PaginationByRequestModel(searchModel).Select(widget =>
                {
                    //fill in model values from the entity
                    var widgetMethodModel = widget.ToPluginModel<WidgetModel>();

                    //fill in additional values (not existing in the entity)
                    widgetMethodModel.IsActive = _widgetService.IsWidgetActive(widget);
                    widgetMethodModel.ConfigurationUrl = widget.GetConfigurationPageUrl();

                    return widgetMethodModel;
                }),
                Total = widgets.Count()
            };

            return model;
        }

        #endregion
    }
}