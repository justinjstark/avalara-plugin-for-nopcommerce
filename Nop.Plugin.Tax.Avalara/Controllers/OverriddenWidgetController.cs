using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Cms;
using Nop.Core.Plugins;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Extensions;
using Nop.Web.Framework.Kendoui;

namespace Nop.Plugin.Tax.Avalara.Controllers
{
    public class OverriddenWidgetController : WidgetController
    {
        #region Fields

        private readonly IPermissionService _permissionService;
        private readonly IWidgetService _widgetService;
        private readonly WidgetSettings _widgetSettings;

        #endregion

        #region Ctor

        public OverriddenWidgetController(IWidgetService widgetService,
            IPermissionService permissionService,
            ISettingService settingService,
            WidgetSettings widgetSettings,
            IPluginFinder pluginFinder) : base(widgetService,
                permissionService,
                settingService,
                widgetSettings,
                pluginFinder)
        {
            this._permissionService = permissionService;
            this._widgetService = widgetService;
            this._widgetSettings = widgetSettings;
        }

        #endregion

        #region Methods

        public override IActionResult List()
        {
            //return base action result
            this.RouteData.Values["controller"] = "Widget";
            return base.List();
        }

        [HttpPost]
        public override IActionResult List(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedKendoGridJson();

            //exclude Avalara tax provider from the widget list
            var widgets = _widgetService.LoadAllWidgets()
                .Where(widget => !widget.PluginDescriptor.SystemName.Equals(AvalaraTaxDefaults.SystemName));

            //prepare model
            var widgetsModel = widgets.Select(widget =>
            {
                var model = widget.ToModel();
                model.IsActive = widget.IsWidgetActive(_widgetSettings);
                model.ConfigurationUrl = widget.GetConfigurationPageUrl();
                return model;
            }).ToList();

            return Json(new DataSourceResult
            {
                Data = widgetsModel,
                Total = widgetsModel.Count
            });
        }

        #endregion
    }
}