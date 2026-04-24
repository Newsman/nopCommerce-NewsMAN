using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Widgets.NewsmanRemarketing.Components
{
    public class WidgetsNewsmanViewComponent : NopViewComponent
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICategoryService _categoryService;
        private readonly ICurrencyService _currencyService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public WidgetsNewsmanViewComponent(
            CurrencySettings currencySettings,
            ICategoryService categoryService,
            ICurrencyService currencyService,
            ILogger logger,
            IOrderService orderService,
            IProductService productService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _currencySettings = currencySettings;
            _categoryService = categoryService;
            _currencyService = currencyService;
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

            return text
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }

        private async Task<NewsmanRemarketingSettings> GetCurrentSettingsAsync()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            return await _settingService.LoadSettingAsync<NewsmanRemarketingSettings>(store.Id);
        }

        private async Task<string> GetCurrentCurrencyCodeAsync()
        {
            var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
            return currency?.CurrencyCode ?? "USD";
        }

        private string GetCurrentBaseUrl()
        {
            return $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}";
        }

        private async Task<Order> GetLastOrderAsync()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();

            return (await _orderService.SearchOrdersAsync(
                storeId: store.Id,
                customerId: customer.Id,
                pageSize: 1)).FirstOrDefault();
        }

        private static int? TryParseRouteInt(object value)
        {
            if (value is int intValue && intValue > 0)
                return intValue;

            if (value is string stringValue && int.TryParse(stringValue, out var parsed) && parsed > 0)
                return parsed;

            return null;
        }

        private static int? TryGetProductIdFromObject(object target, int depth = 0)
        {
            if (target == null || depth > 2)
                return null;

            var targetType = target.GetType();

            foreach (var propertyName in new[] { "ProductId", "Id" })
            {
                var property = targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.GetIndexParameters().Length > 0)
                    continue;

                var value = property.GetValue(target);
                if (value is int intValue && intValue > 0)
                    return intValue;

            }

            foreach (var propertyName in new[] { "ProductModel", "ProductDetailsModel", "Model", "Product", "ProductDetails" })
            {
                var property = targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.GetIndexParameters().Length > 0)
                    continue;

                var nested = property.GetValue(target);
                var nestedId = TryGetProductIdFromObject(nested, depth + 1);
                if (nestedId.HasValue)
                    return nestedId;
            }

            return null;
        }

        private async Task<Product> GetCurrentProductAsync(object additionalData)
        {
            var routeValues = Url.ActionContext.RouteData.Values;
            var productId =
                TryParseRouteInt(routeValues.TryGetValue("productId", out var productIdValue) ? productIdValue : null) ??
                TryParseRouteInt(routeValues.TryGetValue("id", out var idValue) ? idValue : null) ??
                TryGetProductIdFromObject(additionalData);

            if (!productId.HasValue)
                return null;

            return await _productService.GetProductByIdAsync(productId.Value);
        }

        private async Task<string> GetBootstrapScriptAsync(NewsmanRemarketingSettings settings, string currencyCode)
        {
            var cartUrl = $"{GetCurrentBaseUrl()}/Plugins/Newsman/Cart?newsman=getCart.json";
            var remarketingId = FixIllegalJavaScriptChars(settings.NewsmanRemarketingId);
            var escapedCurrencyCode = FixIllegalJavaScriptChars(currencyCode);
            var escapedCartUrl = FixIllegalJavaScriptChars(cartUrl);

            return $$"""
                <script type="text/javascript">
                var _nzm = _nzm || [];
                var _nzm_config = _nzm_config || [];
                _nzm_config["disable_datalayer"] = 1;
                </script>
                <script type="text/javascript">
                var _nzm = _nzm || [];
                var _nzm_config = _nzm_config || [];
                (function(w, d, e, f, c, l, n) {
                    ["identify", "track", "run"].map(function(m) {
                        w[f][m] = function() {
                            w[f].push([m].concat([].slice.call(arguments)));
                        };
                    });
                    l = d.createElement(e);
                    l.async = 1;
                    l.id = "nzm-tracker";
                    l.src = (w[c].js_prefix || "https://t.newsmanapp.com") + "/jt/t.js";
                    l.setAttribute("data-site-id", "{{remarketingId}}");
                    n = d.getElementsByTagName(e)[0];
                    n.parentNode.insertBefore(l, n);
                })(window, document, "script", "_nzm", "_nzm_config");
                </script>
                <script type="text/javascript">
                _nzm.run("require", "ec");
                _nzm.run("set", "currencyCode", "{{escapedCurrencyCode}}");

                var ajaxurl = "{{escapedCartUrl}}";
                var isProd = true;
                var lastCart = sessionStorage.getItem("lastCart");
                if (lastCart === null) {
                    lastCart = {};
                }
                var lastCartFlag = false;
                var firstLoad = true;
                var bufferedXHR = false;
                var unlockClearCart = true;
                var isError = false;
                var secondsAllow = 5;
                var msRunAutoEvents = 5000;
                var msClick = new Date();
                var documentComparer = document.location.hostname;
                var documentUrl = document.URL;
                var sameOrigin = (documentUrl.indexOf(documentComparer) !== -1);
                var startTime, endTime;

                function startTimePassed() {
                    startTime = new Date();
                }

                startTimePassed();

                function endTimePassed() {
                    var flag = false;
                    endTime = new Date();
                    var timeDiff = endTime - startTime;
                    timeDiff /= 1000;
                    var seconds = Math.round(timeDiff);
                    if (firstLoad) {
                        flag = true;
                    }
                    if (seconds >= secondsAllow) {
                        flag = true;
                    }
                    return flag;
                }

                if (sameOrigin) {
                    NewsmanAutoEvents();
                    setInterval(NewsmanAutoEvents, msRunAutoEvents);
                    detectClicks();
                    detectXHR();
                    detectFetch();
                }

                function timestampGenerator(min, max) {
                    min = Math.ceil(min);
                    max = Math.floor(max);
                    return Math.floor(Math.random() * (max - min + 1)) + min;
                }

                function NewsmanAutoEvents() {
                    if (!endTimePassed()) {
                        return;
                    }
                    if (isError && isProd === true) {
                        return;
                    }
                    var xhr = new XMLHttpRequest();
                    if (bufferedXHR || firstLoad) {
                        var paramChar = "?t=";
                        if (ajaxurl.indexOf("?") >= 0) {
                            paramChar = "&t=";
                        }
                        var timestamp = paramChar + Date.now() + timestampGenerator(999, 999999999);
                        try {
                            xhr.open("GET", ajaxurl + timestamp, true);
                        } catch (ex) {
                            isError = true;
                        }
                        startTimePassed();
                        xhr.onload = function () {
                            if (xhr.status == 200 || xhr.status == 201) {
                                var response;
                                try {
                                    response = JSON.parse(xhr.responseText);
                                } catch (error) {
                                    isError = true;
                                    return;
                                }
                                lastCart = JSON.parse(sessionStorage.getItem("lastCart"));
                                if (lastCart === null) {
                                    lastCart = {};
                                }
                                if ((typeof lastCart !== "undefined") && lastCart.length > 0 && (typeof response !== "undefined") && response.length > 0) {
                                    var objComparer = response;
                                    var missingProp = false;
                                    lastCart.forEach(function (e) {
                                        if (!e.hasOwnProperty("name")) {
                                            missingProp = true;
                                        }
                                    });
                                    if (missingProp) {
                                        objComparer.forEach(function (v) {
                                            delete v.name;
                                        });
                                    }
                                    if (JSON.stringify(lastCart) === JSON.stringify(objComparer)) {
                                        lastCartFlag = true;
                                    } else {
                                        lastCartFlag = false;
                                    }
                                }
                                if (response.length > 0 && lastCartFlag == false) {
                                    nzmAddToCart(response);
                                } else if (response.length == 0 && lastCart.length > 0 && unlockClearCart) {
                                    nzmClearCart();
                                }
                                firstLoad = false;
                                bufferedXHR = false;
                            } else {
                                isError = true;
                            }
                        };
                        try {
                            xhr.send(null);
                        } catch (ex) {
                            isError = true;
                        }
                    }
                }

                function nzmClearCart() {
                    _nzm.run("ec:setAction", "clear_cart");
                    _nzm.run("send", "event", "detail view", "click", "clearCart");
                    sessionStorage.setItem("lastCart", JSON.stringify([]));
                    unlockClearCart = false;
                }

                function nzmAddToCart(response) {
                    _nzm.run("ec:setAction", "clear_cart");
                    detailviewEvent(response);
                }

                function detailviewEvent(response) {
                    _nzm.run("send", "event", "detail view", "click", "clearCart", null, function () {
                        var products = [];
                        for (var item in response) {
                            if (response[item].hasOwnProperty("id")) {
                                _nzm.run("ec:addProduct", response[item]);
                                products.push(response[item]);
                            }
                        }
                        _nzm.run("ec:setAction", "add");
                        _nzm.run("send", "event", "UX", "click", "add to cart");
                        sessionStorage.setItem("lastCart", JSON.stringify(products));
                        unlockClearCart = true;
                    });
                }

                function detectClicks() {
                    window.addEventListener("click", function () {
                        msClick = new Date();
                    }, false);
                }

                function detectXHR() {
                    var proxied = window.XMLHttpRequest.prototype.send;
                    window.XMLHttpRequest.prototype.send = function () {
                        var pointer = this;
                        var validate = false;
                        var timeValidate = false;
                        var intervalId = window.setInterval(function () {
                            if (pointer.readyState != 4) {
                                return;
                            }
                            var msClickPassed = new Date();
                            var timeDiff = msClickPassed.getTime() - msClick.getTime();
                            if (timeDiff > 5000) {
                                validate = false;
                            } else {
                                timeValidate = true;
                            }
                            var locationValue = pointer.responseURL || "";
                            if (timeValidate) {
                                if (locationValue.indexOf("newsman=getCart.json") >= 0) {
                                    validate = false;
                                } else if (locationValue.indexOf(window.location.origin) !== -1) {
                                    validate = true;
                                }
                                if (validate) {
                                    bufferedXHR = true;
                                    NewsmanAutoEvents();
                                }
                            }
                            clearInterval(intervalId);
                        }, 1);
                        return proxied.apply(this, [].slice.call(arguments));
                    };
                }

                function detectFetch() {
                    if (typeof window.fetch !== "function") {
                        return;
                    }
                    var origFetch = window.fetch;
                    window.fetch = function () {
                        var reqUrl = "";
                        try {
                            var a0 = arguments[0];
                            reqUrl = typeof a0 === "string" ? a0 : (a0 && a0.url) || "";
                        } catch (e) {}

                        var promise = origFetch.apply(this, arguments);
                        promise.then(function (response) {
                            var validate = false;
                            var timeValidate = false;
                            var msClickPassed = new Date();
                            var timeDiff = msClickPassed.getTime() - msClick.getTime();
                            if (timeDiff > 5000) {
                                validate = false;
                            } else {
                                timeValidate = true;
                            }

                            var locationValue = (response && response.url) || reqUrl;
                            if (timeValidate) {
                                if (locationValue.indexOf("newsman=getCart.json") >= 0) {
                                    validate = false;
                                } else if (locationValue.indexOf(window.location.origin) !== -1) {
                                    validate = true;
                                }
                                if (validate) {
                                    bufferedXHR = true;
                                    NewsmanAutoEvents();
                                }
                            }
                        }).catch(function () {});

                        return promise;
                    };
                }
                </script>
                """;
        }

        private async Task<string> GetProductDetailsScriptAsync(Product product)
        {
            if (product == null)
                return string.Empty;

            var usCulture = new CultureInfo("en-US");
            var category = (await _categoryService.GetCategoryByIdAsync(
                (await _categoryService.GetProductCategoriesByProductIdAsync(product.Id)).FirstOrDefault()?.CategoryId ?? 0))?.Name ?? string.Empty;

            return $$"""
                <script type="text/javascript">
                _nzm.run("ec:addProduct", {
                    "id": "{{FixIllegalJavaScriptChars(product.Id.ToString())}}",
                    "name": "{{FixIllegalJavaScriptChars(product.Name)}}",
                    "category": "{{FixIllegalJavaScriptChars(category)}}",
                    "price": "{{product.Price.ToString("0.00", usCulture)}}"
                });
                _nzm.run("ec:setAction", "detail");
                _nzm.run("send", "pageview");
                </script>
                """;
        }

        private async Task<string> GetPurchaseScriptAsync(Order order)
        {
            if (order == null)
                return string.Empty;

            var store = await _storeContext.GetCurrentStoreAsync();
            var usCulture = new CultureInfo("en-US");
            var sb = new StringBuilder();

            foreach (var item in await _orderService.GetOrderItemsAsync(order.Id))
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product == null)
                    continue;

                var category = (await _categoryService.GetCategoryByIdAsync(
                    (await _categoryService.GetProductCategoriesByProductIdAsync(item.ProductId)).FirstOrDefault()?.CategoryId ?? 0))?.Name ?? string.Empty;

                sb.AppendLine($$"""
                    _nzm.run("ec:addProduct", {
                        "id": "{{FixIllegalJavaScriptChars(product.Id.ToString())}}",
                        "name": "{{FixIllegalJavaScriptChars(product.Name)}}",
                        "category": "{{FixIllegalJavaScriptChars(category)}}",
                        "price": "{{item.UnitPriceInclTax.ToString("0.00", usCulture)}}",
                        "quantity": {{item.Quantity}}
                    });
                    """);
            }

            return $$"""
                <script type="text/javascript">
                {{sb}}
                _nzm.run("ec:setAction", "purchase", {
                    "id": "{{FixIllegalJavaScriptChars(order.CustomOrderNumber)}}",
                    "affiliation": "{{FixIllegalJavaScriptChars(store.Name)}}",
                    "revenue": {{order.OrderTotal.ToString("0.00", usCulture)}},
                    "tax": {{order.OrderTax.ToString("0.00", usCulture)}},
                    "shipping": {{order.OrderShippingInclTax.ToString("0.00", usCulture)}}
                });
                _nzm.run("send", "pageview");
                </script>
                """;
        }

        private static string GetPageViewScript()
        {
            return """
                <script type="text/javascript">
                _nzm.run("send", "pageview");
                </script>
                """;
        }

        #endregion

        #region Methods

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            try
            {
                var routeData = Url.ActionContext.RouteData;
                var controller = routeData.Values["controller"]?.ToString();
                var action = routeData.Values["action"]?.ToString();

                if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action))
                    return Content(string.Empty);

                var settings = await GetCurrentSettingsAsync();
                if (string.IsNullOrWhiteSpace(settings.NewsmanRemarketingId))
                    return Content(string.Empty);

                var script = new StringBuilder();
                script.AppendLine(await GetBootstrapScriptAsync(settings, await GetCurrentCurrencyCodeAsync()));

                var isOrderCompletedPage =
                    (controller.Equals("checkout", StringComparison.InvariantCultureIgnoreCase) &&
                     action.Equals("completed", StringComparison.InvariantCultureIgnoreCase)) ||
                    (controller.Equals("order", StringComparison.InvariantCultureIgnoreCase) &&
                     action.Equals("details", StringComparison.InvariantCultureIgnoreCase));

                var isProductDetailsPage =
                    controller.Equals("product", StringComparison.InvariantCultureIgnoreCase) &&
                    action.Equals("productdetails", StringComparison.InvariantCultureIgnoreCase);

                if (isOrderCompletedPage && settings.EnableEcommerce)
                {
                    script.AppendLine(await GetPurchaseScriptAsync(await GetLastOrderAsync()));
                }
                else if (isProductDetailsPage)
                {
                    script.AppendLine(await GetProductDetailsScriptAsync(await GetCurrentProductAsync(additionalData)));
                }
                else
                {
                    script.AppendLine(GetPageViewScript());
                }

                return View("~/Plugins/Widgets.NewsmanRemarketing/Views/PublicInfo.cshtml", script.ToString());
            }
            catch (Exception ex)
            {
                await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Error, "Error creating scripts for Newsman tracking", ex.ToString());
                return Content(string.Empty);
            }
        }

        #endregion
    }
}
