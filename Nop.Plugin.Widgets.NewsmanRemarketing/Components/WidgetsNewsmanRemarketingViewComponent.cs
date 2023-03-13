using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Widgets.NewsmanRemarketing;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Widgets.NewsmanRemarketing.Components
{
    public class WidgetsNewsmanViewComponent : NopViewComponent
    {
        #region Fields

        private const string ORDER_ALREADY_PROCESSED_ATTRIBUTE_NAME = "Newsman.OrderAlreadyProcessed";

        private readonly CurrencySettings _currencySettings;
        private readonly NewsmanRemarketingSettings _newsmanSettings;
        private readonly ICategoryService _categoryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public WidgetsNewsmanViewComponent(CurrencySettings currencySettings,
            NewsmanRemarketingSettings newsmanSettings,
            ICategoryService categoryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            ILogger logger,
            IOrderService orderService,
            IProductService productService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _currencySettings = currencySettings;
            _newsmanSettings = newsmanSettings;
            _categoryService = categoryService;
            _currencyService = currencyService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _logger = logger;
            _orderService = orderService;
            _productService = productService;
            _settingService = settingService;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        private string FixIllegalJavaScriptChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            //replace ' with \' (http://stackoverflow.com/questions/4292761/need-to-url-encode-labels-when-tracking-events-with-google-analytics)
            text = text.Replace("'", "\\'");
            return text;
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task<Order> GetLastOrderAsync()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();
            var order = (await _orderService.SearchOrdersAsync(storeId: store.Id,
                customerId: customer.Id, pageSize: 1)).FirstOrDefault();
            return order;
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task<string> GetEcommerceScriptAsync(Order order)
        {
            var analyticsTrackingScript = @"<script>//Newsman remarketing tracking code REPLACEABLE

		var remarketingid = '" + _newsmanSettings.NewsmanRemarketingId + "';" +
        @"var _nzmPluginInfo = '1.0.0:nopcommerce';
		
		//Newsman remarketing tracking code REPLACEABLE

		//Newsman remarketing tracking code  

		var endpoint = 'https://retargeting.newsmanapp.com';
		var remarketingEndpoint = endpoint + '/js/retargeting/track.js';

		var _nzm = _nzm || [];
		var _nzm_config = _nzm_config || [];
		_nzm_config['disable_datalayer'] = 1;
		_nzm_tracking_server = endpoint;
		(function() {
			var a, methods, i;
			a = function(f) {
				return function() {
					_nzm.push([f].concat(Array.prototype.slice.call(arguments, 0)));
				}
			};
			methods = ['identify', 'track', 'run'];
			for (i = 0; i < methods.length; i++) {
				_nzm[methods[i]] = a(methods[i])
			};
			s = document.getElementsByTagName('script')[0];
			var script_dom = document.createElement('script');
			script_dom.async = true;
			script_dom.id = 'nzm-tracker';
			script_dom.setAttribute('data-site-id', remarketingid);
			script_dom.src = remarketingEndpoint;
			//check for engine name
			if (_nzmPluginInfo.indexOf('shopify') !== -1) {
				script_dom.onload = function(){
					if (typeof newsmanRemarketingLoad === 'function')
						newsmanRemarketingLoad();
				}
			}
			s.parentNode.insertBefore(script_dom, s);
		})();
		_nzm.run('require', 'ec');

		//Newsman remarketing tracking code     

		//Newsman remarketing auto events REPLACEABLE

		var ajaxurl = 'https://' + document.location.hostname + '/Plugins/Newsman/Cart?newsman=getCart.json';

		//Newsman remarketing auto events REPLACEABLE

		//Newsman remarketing auto events

		var isProd = true;

		let lastCart = sessionStorage.getItem('lastCart');
		if (lastCart === null)
			lastCart = {};

		var lastCartFlag = false;
		var firstLoad = true;
		var bufferedXHR = false;
		var unlockClearCart = true;
		var isError = false;
		var documentComparer = document.location.hostname;
		var documentUrl = document.URL;
		var sameOrigin = (documentUrl.indexOf(documentComparer) !== -1);

		let startTime, endTime;

		function startTimePassed() {
			startTime = new Date();
		};

		startTimePassed();

		function endTimePassed() {
			var flag = false;

			endTime = new Date();
			var timeDiff = endTime - startTime;

			timeDiff /= 1000;

			var seconds = Math.round(timeDiff);

			if (firstLoad)
				flag = true;

			if (seconds >= 5)
				flag = true;

			return flag;
		}

		if (sameOrigin) {
			NewsmanAutoEvents();
			setInterval(NewsmanAutoEvents, 5000);

			detectXHR();
		}

		function timestampGenerator(min, max) {
			min = Math.ceil(min);
			max = Math.floor(max);
			return Math.floor(Math.random() * (max - min + 1)) + min;
		}

		function NewsmanAutoEvents() {

			if (!endTimePassed())
			{
				if (!isProd)
					console.log('newsman remarketing: execution stopped at the beginning, 5 seconds didn\""t pass between requests');

				return;
			}

			if (isError && isProd == true) {
				console.log('newsman remarketing: an error occurred, set isProd = false in console, script execution stopped;');

				return;
			}

			let xhr = new XMLHttpRequest()

			if (bufferedXHR || firstLoad) {

			var paramChar = '?t=';

			if (ajaxurl.indexOf('?') >= 0)
				paramChar = '&t=';

			var timestamp = paramChar + Date.now() + timestampGenerator(999, 999999999);

			try{
				xhr.open('GET', ajaxurl + timestamp, true);
			}
			catch(ex){
				if (!isProd)
				console.log('newsman remarketing: malformed XHR url');

				isError = true;
			}

				startTimePassed();

				xhr.onload = function() {

					if (xhr.status == 200 || xhr.status == 201) {

						try {
							var response = JSON.parse(xhr.responseText);
						} catch (error) {
							if (!isProd)
								console.log('newsman remarketing: error occured json parsing response');

							isError = true;

							return;
						}

						//check for engine name
						if (_nzmPluginInfo.indexOf('shopify') !== -1) {

							var products = [];

							if(response.item_count > 0)
							{
								response.items.forEach(function(item){
								
									products.push(
										{
											'id': item.id,
											'name': item.product_title,
											'quantity': item.quantity,
											'price': item.price
										}
									);

								});
							}

							response = products;
						}

						lastCart = JSON.parse(sessionStorage.getItem('lastCart'));

						if (lastCart === null)
							lastCart = {};

						//check cache
						if (lastCart.length > 0 && lastCart != null && lastCart != undefined && response.length > 0 && response != null && response != undefined) {
							if (JSON.stringify(lastCart) === JSON.stringify(response)) {
								if (!isProd)
									console.log('newsman remarketing: cache loaded, cart is unchanged');

								lastCartFlag = true;
							} else {
								lastCartFlag = false;

								if (!isProd)
									console.log('newsman remarketing: cache loaded, cart is changed');
							}
						}

						if (response.length > 0 && lastCartFlag == false) {

							addToCart(response);

						}
						//send only when on last request, products existed
						else if (response.length == 0 && lastCart.length > 0 && unlockClearCart) {

							clearCart();

							if (!isProd)
								console.log('newsman remarketing: clear cart sent');

						} else {

							if (!isProd)
								console.log('newsman remarketing: request not sent');

						}

						firstLoad = false;
						bufferedXHR = false;

					} else {
						if (!isProd)
							console.log('newsman remarketing: response http status code is not 200');

						isError = true;
					}

				}

				try{
					xhr.send(null);
				}
				catch(ex){
					if (!isProd)
					console.log('newsman remarketing: error on xhr send');

				isError = true;
				}

			} else {
				if (!isProd)
					console.log('newsman remarketing: !buffered xhr || first load');
			}

		}

		function clearCart() {

			_nzm.run('ec:setAction', 'clear_cart');
			_nzm.run('send', 'event', 'detail view', 'click', 'clearCart');

			sessionStorage.setItem('lastCart', JSON.stringify([]));

			unlockClearCart = false;

		}

		function addToCart(response) {

			_nzm.run('ec:setAction', 'clear_cart');
			_nzm.run('send', 'event', 'detail view', 'click', 'clearCart', null, _nzm.createFunctionWithTimeout(function() {

				for (var item in response) {

					_nzm.run('ec:addProduct',
						response[item]
					);

				}

				_nzm.run('ec:setAction', 'add');
				_nzm.run('send', 'event', 'UX', 'click', 'add to cart');

				sessionStorage.setItem('lastCart', JSON.stringify(response));
				unlockClearCart = true;

				if (!isProd)
					console.log('newsman remarketing: cart sent');

			}));

		}

		function detectXHR() {

			var proxied = window.XMLHttpRequest.prototype.send;
			window.XMLHttpRequest.prototype.send = function() {

				var pointer = this;
				var validate = false;
				var intervalId = window.setInterval(function() {

					if (pointer.readyState != 4) {
						return;
					}

					var _location = pointer.responseURL;

					//own request exclusion
					if (
									_location.indexOf('getCart.json') >= 0 ||
									//magento 2.x
									_location.indexOf('/static/') >= 0 ||
									_location.indexOf('/pub/static') >= 0 ||
									_location.indexOf('/customer/section') >= 0 ||
									//opencart 1
									_location.indexOf('getCart=true') >= 0 ||
									//shopify
									_location.indexOf('cart.js') >= 0
					) {
						validate = false;
					} else {
						//check for engine name
						if (_nzmPluginInfo.indexOf('shopify') !== -1) {
							validate = true;
						}
						else{
							if (_location.indexOf(window.location.origin) !== -1)
							validate = true;
						}
					}

					if (validate) {
						bufferedXHR = true;

						if (!isProd)
							console.log('newsman remarketing: ajax request fired and catched from same domain');

						NewsmanAutoEvents();
					}

					clearInterval(intervalId);

				}, 1);

				return proxied.apply(this, [].slice.call(arguments));
			};

		}

		//Newsman remarketing auto events";

            //ecommerce info
            var store = await _storeContext.GetCurrentStoreAsync();
            var settings = await _settingService.LoadSettingAsync<NewsmanRemarketingSettings>(store.Id);
            if (order != null)
            {
                var usCulture = new CultureInfo("en-US");

                var analyticsEcommerceScript = @"
				{DETAILS}
				_nzm.run('ec:setAction', 'purchase', {
                    'id': '{ORDERID}',
                    'affiliation': '{SITE}',
                    'revenue': {TOTAL},
                    'tax': {TAX},
                    'shipping': {SHIP}
                });";
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{ORDERID}", FixIllegalJavaScriptChars(order.CustomOrderNumber));
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{SITE}", FixIllegalJavaScriptChars(store.Name));
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{TOTAL}", order.OrderTotal.ToString("0.00", usCulture));
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{TAX}", order.OrderTax.ToString("0.00", usCulture));
                var orderShipping = order.OrderShippingInclTax;
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{SHIP}", orderShipping.ToString("0.00", usCulture));

                var sb = new StringBuilder();
                var listingPosition = 1;
                foreach (var item in await _orderService.GetOrderItemsAsync(order.Id))
                {
                    if (!string.IsNullOrEmpty(sb.ToString()))
                        sb.AppendLine(",");

                    var analyticsEcommerceDetailScript = @"_nzm.run( 'ec:addProduct',{
                    'id': '{PRODUCTSKU}',
                    'name': '{PRODUCTNAME}',
                    'category': '{CATEGORYNAME}',
                    'price': '{UNITPRICE}',
                    'quantity': {QUANTITY}
                    });
                    ";

                    var product = await _productService.GetProductByIdAsync(item.ProductId);

                    var sku = await _productService.FormatSkuAsync(product, item.AttributesXml);

                    if (string.IsNullOrEmpty(sku))
                        sku = product.Id.ToString();

                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{PRODUCTSKU}", FixIllegalJavaScriptChars(product.Id.ToString()));
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{PRODUCTNAME}", FixIllegalJavaScriptChars(product.Name));
                    var category = (await _categoryService.GetCategoryByIdAsync((await _categoryService.GetProductCategoriesByProductIdAsync(item.ProductId)).FirstOrDefault()?.CategoryId ?? 0))?.Name;
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{CATEGORYNAME}", FixIllegalJavaScriptChars(category));
                    var unitPrice = item.UnitPriceInclTax;
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{QUANTITY}", item.Quantity.ToString());
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{UNITPRICE}", unitPrice.ToString("0.00", usCulture));
                    sb.AppendLine(analyticsEcommerceDetailScript);

                    listingPosition++;
                }

                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{DETAILS}", sb.ToString());

                analyticsEcommerceScript += "_nzm.run('send', 'pageview');";

                analyticsTrackingScript += analyticsEcommerceScript;
            }

            return analyticsTrackingScript + "</script>";
        }

        #endregion

        #region Methods

        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            var script = "";
            var routeData = Url.ActionContext.RouteData;

            try
            {
                var controller = routeData.Values["controller"];
                var action = routeData.Values["action"];

                if (controller == null || action == null)
                    return Content("");

                var isOrderCompletedPage = controller.ToString().Equals("checkout", StringComparison.InvariantCultureIgnoreCase) &&
                    action.ToString().Equals("completed", StringComparison.InvariantCultureIgnoreCase);

				if(!isOrderCompletedPage)
					isOrderCompletedPage = controller.ToString().Equals("order", StringComparison.InvariantCultureIgnoreCase) &&
                    action.ToString().Equals("details", StringComparison.InvariantCultureIgnoreCase);

                if (isOrderCompletedPage && _newsmanSettings.EnableEcommerce)
                {
                    var lastOrder = await GetLastOrderAsync();
                    script += await GetEcommerceScriptAsync(lastOrder);
                }
                else
                {
                    script += await GetEcommerceScriptAsync(null);
                }
            }
            catch (Exception ex)
            {
                await _logger.InsertLogAsync(Core.Domain.Logging.LogLevel.Error, "Error creating scripts for Newsman tracking", ex.ToString());
            }
            return View("~/Plugins/Widgets.NewsmanRemarketing/Views/PublicInfo.cshtml", script);
        }

        #endregion
    }
}