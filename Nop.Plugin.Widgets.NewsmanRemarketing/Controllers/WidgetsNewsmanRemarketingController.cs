using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Widgets.NewsmanRemarketing.Models;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Widgets.NewsmanRemarketing.Controllers
{
    [Area(AreaNames.Admin)]
    [AuthorizeAdmin]
    [AutoValidateAntiforgeryToken]
    public class WidgetsNewsmanRemarketingController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;
        private readonly IProductService _productService;

        #endregion

        #region Ctor

        public WidgetsNewsmanRemarketingController(
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            IShoppingCartService shoppingCartService,
            IWorkContext workContext,
            IProductService productService)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
            _productService = productService;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var newsmanRemarketingSettings = await _settingService.LoadSettingAsync<NewsmanRemarketingSettings>(storeScope);

            var model = new ConfigurationModel
            {
                EnableEcommerce = newsmanRemarketingSettings.EnableEcommerce,
                RemarketingId = newsmanRemarketingSettings.NewsmanRemarketingId,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.RemarketingId_OverrideForStore = await _settingService.SettingExistsAsync(newsmanRemarketingSettings, x => x.NewsmanRemarketingId, storeScope);
                model.EnableEcommerce_OverrideForStore = await _settingService.SettingExistsAsync(newsmanRemarketingSettings, x => x.EnableEcommerce, storeScope);
            }

            return View("~/Plugins/Widgets.NewsmanRemarketing/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var newsmanRemarketingSettings = await _settingService.LoadSettingAsync<NewsmanRemarketingSettings>(storeScope);

            newsmanRemarketingSettings.NewsmanRemarketingId = model.RemarketingId;
            newsmanRemarketingSettings.EnableEcommerce = model.EnableEcommerce;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(newsmanRemarketingSettings, x => x.NewsmanRemarketingId, model.RemarketingId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(newsmanRemarketingSettings, x => x.EnableEcommerce, model.EnableEcommerce_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}