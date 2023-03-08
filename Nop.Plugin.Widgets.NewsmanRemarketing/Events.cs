using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Events;
using Nop.Core.Http;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Stores;

namespace Nop.Plugin.Widgets.NewsmanRemarketing
{
    public class EventConsumer :
        IConsumer<OrderStatusChangedEvent>,
        IConsumer<OrderPaidEvent>,
        IConsumer<EntityDeletedEvent<Order>>

    {
        private readonly IAddressService _addressService;
        private readonly ICategoryService _categoryService;
        private readonly ICountryService _countryService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IWebHelper _webHelper;
        private readonly IWidgetPluginManager _widgetPluginManager;

        public EventConsumer(IAddressService addressService,
            ICategoryService categoryService,
            ICountryService countryService,
            IHttpClientFactory httpClientFactory,
            ILogger logger,
            IOrderService orderService,
            IProductService productService,
            ISettingService settingService,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            IStoreService storeService,
            IWebHelper webHelper,
            IWidgetPluginManager widgetPluginManager)
        {
            _addressService = addressService;
            _categoryService = categoryService;
            _countryService = countryService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _orderService = orderService;
            _productService = productService;
            _settingService = settingService;
            _stateProvinceService = stateProvinceService;
            _storeContext = storeContext;
            _storeService = storeService;
            _webHelper = webHelper;
            _widgetPluginManager = widgetPluginManager;
        }

        private string FixIllegalJavaScriptChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            //replace ' with \' (http://stackoverflow.com/questions/4292761/need-to-url-encode-labels-when-tracking-events-with-google-analytics)
            text = text.Replace("'", "\\'");
            return text;
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task<bool> IsPluginEnabledAsync()
        {
            return await _widgetPluginManager.IsPluginActiveAsync("Widgets.NewsmanRemarketing");
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task ProcessOrderEventAsync(Order order, bool add)
        {
        }

        /// <summary>
        /// Handles the event
        /// </summary>
        /// <param name="eventMessage">The event message</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(EntityDeletedEvent<Order> eventMessage)
        {
        }

        /// <summary>
        /// Handles the event
        /// </summary>
        /// <param name="eventMessage">The event message</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(OrderStatusChangedEvent eventMessage)
        {
        }

        /// <summary>
        /// Handles the event
        /// </summary>
        /// <param name="eventMessage">The event message</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(OrderPaidEvent eventMessage)
        {
        }
    }
}