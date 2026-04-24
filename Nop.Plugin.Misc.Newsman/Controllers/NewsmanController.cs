using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Plugin.Misc.Newsman.Models;
using Nop.Plugin.Misc.Newsman.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.ScheduleTasks;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Nop.Services.Catalog;
using Product = Nop.Plugin.Misc.Newsman.Models.Product;
using Cart = Nop.Plugin.Misc.Newsman.Models.Cart;
using Order = Nop.Plugin.Misc.Newsman.Models.Order;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Orders;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Misc.Newsman.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class NewsmanController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ISettingService _settingService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IPictureService _pictureService;
        private readonly IUrlRecordService _urlRecordRepository;
        private readonly IAddressService _addressService;
        private readonly ICustomerService _customerService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;
        private readonly NewsmanManager _newsmanManager;

        #endregion

        #region Ctor

        public NewsmanController(
            ILocalizationService localizationService,
            INotificationService notificationService,
            IScheduleTaskService scheduleTaskService,
            ISettingService settingService,
            IStaticCacheManager cacheManager,
            IStoreContext storeContext,
            IStoreService storeService,
            IProductService productService,
            IOrderService orderService,
            IPictureService pictureService,
            IUrlRecordService urlRecordRepository,
            IAddressService addressService,
            ICustomerService customerService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            IShoppingCartService shoppingCartService,
            IWorkContext workContext,
            NewsmanManager newsmanManager)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _scheduleTaskService = scheduleTaskService;
            _settingService = settingService;
            _staticCacheManager = cacheManager;
            _storeContext = storeContext;
            _storeService = storeService;
            _productService = productService;
            _pictureService = pictureService;
            _urlRecordRepository = urlRecordRepository;
            _orderService = orderService;
            _addressService = addressService;
            _customerService = customerService;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
            _newsmanManager = newsmanManager;
        }

        #endregion

        #region Methods

        private string GetStoreBaseUrl()
        {
            return $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}";
        }

        private string BuildProductFeedUrl(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return string.Empty;

            return $"{GetStoreBaseUrl()}/Plugins/Newsman/Api?apikey={Uri.EscapeDataString(apiKey)}&type=products.json";
        }

        private bool IsPublicBaseUrl(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                return false;

            var host = uri.Host;
            if (string.IsNullOrWhiteSpace(host))
                return false;

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return false;

            if (IPAddress.TryParse(host, out var address))
            {
                if (IPAddress.IsLoopback(address))
                    return false;

                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var bytes = address.GetAddressBytes();
                    if (bytes[0] == 10 ||
                        bytes[0] == 127 ||
                        (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                        (bytes[0] == 192 && bytes[1] == 168))
                        return false;
                }

                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
                        return false;

                    var bytes = address.GetAddressBytes();
                    if ((bytes[0] & 0xFE) == 0xFC)
                        return false;
                }
            }

            return true;
        }

        private string GetFeedGenerationWarning(string baseUrl, string listId, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "Product feed was not generated because the API key is empty.";

            if (string.IsNullOrWhiteSpace(listId) || listId.Equals(Guid.Empty.ToString(), StringComparison.OrdinalIgnoreCase))
                return "Product feed was not generated because no NewsMAN list is selected.";

            if (!IsPublicBaseUrl(baseUrl))
                return "The product feed URL is not publicly accessible. Automatic feed registration in NewsMAN works only when the store can be reached from the internet. Use a public website URL or a temporary tunnel.";

            return null;
        }

        private async Task<ConfigurationModel> PrepareConfigurationModelAsync(NewsmanSettings newsmanSettings, int storeId, string feedGenerationStatus = null, string feedGenerationWarning = null)
        {
            var baseStoreUrl = GetStoreBaseUrl();
            var model = new ConfigurationModel
            {
                UserId = newsmanSettings.UserId,
                ApiKey = newsmanSettings.ApiKey,
                ImportType = newsmanSettings.ImportType,
                AllowApi = newsmanSettings.AllowApi,
                ListId = newsmanSettings.ListId,
                ListId_OverrideForStore = storeId > 0 && await _settingService.SettingExistsAsync(newsmanSettings, settings => settings.ListId, storeId),
                SegmentId = newsmanSettings.SegmentId,
                SegmentId_OverrideForStore = storeId > 0 && await _settingService.SettingExistsAsync(newsmanSettings, settings => settings.SegmentId, storeId),
                ActiveStoreScopeConfiguration = storeId,
                BaseStoreUrl = baseStoreUrl,
                IsPublicBaseUrl = IsPublicBaseUrl(baseStoreUrl),
                ProductFeedUrl = BuildProductFeedUrl(newsmanSettings.ApiKey),
                FeedGenerationStatus = feedGenerationStatus,
                FeedGenerationWarning = feedGenerationWarning
            };

            model.ImportType = newsmanSettings.ImportType ?? model.AvailableImportTypes.FirstOrDefault().Value;
            model.AllowApi = string.IsNullOrEmpty(newsmanSettings.AllowApi) ? model.AvailableApi.LastOrDefault().Value : newsmanSettings.AllowApi;

            if (!string.IsNullOrEmpty(newsmanSettings.ApiKey) && !string.IsNullOrEmpty(newsmanSettings.UserId))
                model.AvailableLists = _newsmanManager.GetAvailableListsAsync() ?? new List<SelectListItem>();

            var defaultListId = newsmanSettings.ListId;
            if (!model.AvailableLists.Any())
            {
                model.AvailableLists.Add(new SelectListItem
                {
                    Text = "No lists present",
                    Value = Guid.Empty.ToString()
                });
                defaultListId = Guid.Empty.ToString();
            }
            else if (string.IsNullOrEmpty(newsmanSettings.ListId) || newsmanSettings.ListId.Equals(Guid.Empty.ToString()))
            {
                defaultListId = model.AvailableLists.FirstOrDefault()?.Value;
            }

            if (!string.IsNullOrEmpty(newsmanSettings.ListId) && !newsmanSettings.ListId.Equals(Guid.Empty.ToString()))
                model.AvailableSegments = _newsmanManager.GetAvailableSegmentsAsync() ?? new List<SelectListItem>();

            var defaultSegmentId = newsmanSettings.SegmentId;
            if (!model.AvailableSegments.Any())
            {
                model.AvailableSegments.Add(new SelectListItem
                {
                    Text = "No list selected",
                    Value = Guid.Empty.ToString()
                });
                defaultSegmentId = Guid.Empty.ToString();
            }
            else if (string.IsNullOrEmpty(newsmanSettings.SegmentId) || newsmanSettings.SegmentId.Equals(Guid.Empty.ToString()))
            {
                defaultSegmentId = model.AvailableSegments.FirstOrDefault()?.Value;
            }

            model.ListId = defaultListId;
            newsmanSettings.ListId = defaultListId;
            await _settingService.SaveSettingOverridablePerStoreAsync(newsmanSettings, settings => settings.ListId, model.ListId_OverrideForStore, storeId);
            model.SegmentId = defaultSegmentId;
            newsmanSettings.SegmentId = defaultSegmentId;
            await _settingService.SaveSettingOverridablePerStoreAsync(newsmanSettings, settings => settings.SegmentId, model.SegmentId_OverrideForStore, storeId);

            ViewData["syncUrlCron"] = GetStoreBaseUrl() + "/Plugins/Newsman/Sync?apikey=" + model.ApiKey + "&listId=" + model.ListId + "&segmentId=" + model.SegmentId + "&limit=9000&cronLast=true";
            ViewData["syncUrl"] = GetStoreBaseUrl() + "/Plugins/Newsman/Sync?apikey=" + model.ApiKey + "&listId=" + model.ListId + "&segmentId=" + model.SegmentId;

            return model;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            //load settings for a chosen store scope
            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var newsmanSettings = await _settingService.LoadSettingAsync<NewsmanSettings>(storeId);
            var model = await PrepareConfigurationModelAsync(
                newsmanSettings,
                storeId,
                TempData["NewsmanFeedStatus"] as string,
                TempData["NewsmanFeedWarning"] as string);

            return View("~/Plugins/Misc.Newsman/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var newsmanSettings = await _settingService.LoadSettingAsync<NewsmanSettings>(storeId);

            //save settings
            newsmanSettings.UserId = model.UserId?.Trim();
            newsmanSettings.ApiKey = model.ApiKey?.Trim();
            newsmanSettings.ImportType = model.ImportType;
            newsmanSettings.AllowApi = model.AllowApi;
            newsmanSettings.ListId = model.ListId;
            newsmanSettings.SegmentId = model.SegmentId;

            await _settingService.SaveSettingAsync(newsmanSettings, x => x.UserId, clearCache: false);
            await _settingService.SaveSettingAsync(newsmanSettings, x => x.ApiKey, clearCache: false);
            await _settingService.SaveSettingAsync(newsmanSettings, x => x.ImportType, clearCache: false);
            await _settingService.SaveSettingAsync(newsmanSettings, x => x.AllowApi, clearCache: false);
            await _settingService.SaveSettingOverridablePerStoreAsync(newsmanSettings, x => x.ListId, model.ListId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(newsmanSettings, x => x.SegmentId, model.SegmentId_OverrideForStore, storeId, false);
            await _settingService.ClearCacheAsync();

            var baseStoreUrl = GetStoreBaseUrl();
            var productFeedUrl = BuildProductFeedUrl(newsmanSettings.ApiKey);
            var feedWarning = GetFeedGenerationWarning(baseStoreUrl, newsmanSettings.ListId, newsmanSettings.ApiKey);
            string feedStatus;

            if (string.IsNullOrEmpty(feedWarning))
            {
                feedStatus = _newsmanManager.SetFeedList(newsmanSettings.ListId, productFeedUrl, baseStoreUrl);
            }
            else
            {
                feedStatus = "Automatic feed registration was skipped because the generated feed URL is not publicly accessible.";
            }

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            if (!string.IsNullOrWhiteSpace(feedWarning))
                _notificationService.WarningNotification(feedWarning);

            TempData["NewsmanFeedStatus"] = feedStatus;
            TempData["NewsmanFeedWarning"] = feedWarning;

            var configuredModel = await PrepareConfigurationModelAsync(newsmanSettings, storeId, feedStatus, feedWarning);
            return View("~/Plugins/Misc.Newsman/Views/Configure.cshtml", configuredModel);
        }

        public async Task<IActionResult> Api(string apikey, string type, int? product_id, int? order_id, int? start = 1, int? limit = 1000)
        {
            var settings = _newsmanManager.GetSettings();

            if(settings.AllowApi == "disabled" || string.IsNullOrEmpty(settings.AllowApi))
                return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "API disabled" }));

            if (string.IsNullOrEmpty(apikey) || string.IsNullOrEmpty(type))
                return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "parameters missing, apikey, type" }));

            if (settings.ApiKey != apikey)
                return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Forbidden, api keys do not match" }));

            var data = new
            {
                message = "no data present, Type parameter might be incorrect"
            };

            switch (type)
            {
                case "orders.json":

                    List<Order> orders = new List<Order>();

                    var ordersRaw = await _orderService.SearchOrdersAsync();

                    var searchedOrder = new List<Core.Domain.Orders.Order>();

                    if (order_id == null)
                    {
                        searchedOrder = ordersRaw.Skip((int)start).Take((int)limit).ToList();
                    }
                    else
                    {
                        searchedOrder = ordersRaw.Where(item => item.Id == (int)order_id).ToList();
                    }

                    foreach (var order in searchedOrder)
                    {
                        var address = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
                        var orderProducts = new List<Product>();
                        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

                        foreach (var item in orderItems)
                        {
                            orderProducts.Add(await _newsmanManager.GetOrderItems(item, GetStoreBaseUrl()));
                        }

                        orders.Add(
                            new Order()
                            {
                                order_no = order.Id,
                                date = order.CreatedOnUtc.ToFileTimeUtc().ToString(),
                                status = order.OrderStatus.ToString(),
                                lastname = address.LastName,
                                firstname = address.FirstName,
                                email = address.Email,
                                phone = address.PhoneNumber,
                                state = "",
                                city = address.City,
                                address = address.Address1,
                                discount = order.OrderDiscount,
                                shipping = order.OrderShippingInclTax,
                                fees = order.PaymentMethodAdditionalFeeInclTax,
                                rebates = 0,
                                total = order.OrderTotal,
                                products = orderProducts
                            }
                        );
                    }

                    return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(orders, Newtonsoft.Json.Formatting.Indented));
                case "products.json":

                    List<Product> products = new List<Product>();

                    var productsRaw = await _productService.SearchProductsAsync(
                        categoryIds: null
                    );

                    var searchedProducts = new List<Core.Domain.Catalog.Product>();

                    if (product_id == null)
                    {
                       searchedProducts = productsRaw.Skip((int)start).Take((int)limit).ToList();
                    }
                    else
                    {
                       searchedProducts = productsRaw.Where(item => item.Id == (int)product_id).ToList();
                    }

                    foreach(var product in searchedProducts)
                    {
                        var picture = await _pictureService.GetPicturesByProductIdAsync(product.Id);
                        if (picture == null || picture.Count == 0)
                            continue;
                        string imageUrl = await _pictureService.GetPictureUrlAsync(picture.FirstOrDefault().Id);

                        var url = await _urlRecordRepository.GetSeNameAsync(product.Id, "Product");
                        url = GetStoreBaseUrl() + "/" + url;

                        products.Add(
                            new Product()
                            {
                                id = product.Id,
                                name = product.Name,
                                stock_quantity = product.StockQuantity,
                                price = product.Price,
                                price_old = product.OldPrice,
                                image_url = imageUrl,
                                url = url
                            }
                        );
                    }

                    return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(products, Newtonsoft.Json.Formatting.Indented));
                case "customers.json":

                    var customers = (await _customerService.GetAllCustomersAsync())
                        .Select(item => new Customer() { email = item.Email, first_name = item.FirstName, last_name = item.LastName })
                        .ToList();

                    return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(customers, Newtonsoft.Json.Formatting.Indented));
                case "subscribers.json":

                    var subscribers = (await _newsLetterSubscriptionService.GetAllNewsLetterSubscriptionsAsync())
                        .Select(item => new Subscriber() { email = item.Email })
                        .ToList();

                    return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(subscribers, Newtonsoft.Json.Formatting.Indented));
            }

            return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(data));
        }

        public IActionResult Sync(string apikey, string cronlast, int? limit = 10000)
        {
            var settings = _newsmanManager.GetSettings();
            bool cronLast = false;

            if (!string.IsNullOrEmpty(cronlast))
                cronLast = true;

            if(settings.ApiKey != apikey)
                return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Forbidden, api keys do not match" }));

            var message = _newsmanManager.Synchronize(cronLast, limit).Result;

            var status = new
            {
                message = message
            };

            return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(status));
        }

        public async Task<IActionResult> Cart()
        {
            var cart = new List<Cart>();

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var shoppingCarts = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            foreach (var sci in shoppingCarts)
            {
                var _product = await _productService.GetProductByIdAsync(sci.ProductId);

                cart.Add(
                    new Cart()
                    {
                        id = _product.Id,
                        name = _product.Name,
                        price = _product.Price,
                        quantity = sci.Quantity
                    }
                );
            }

            return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(cart));
        }

        #endregion
    }
}
