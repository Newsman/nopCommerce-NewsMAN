using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Messages;
using Nop.Services.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NET_Newsman_API_Wrapper;
using Nop.Plugin.Misc.Newsman.Models;
using System.Collections.Specialized;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Catalog;
using Nop.Services.Media;
using Nop.Services.Seo;
using Product = Nop.Plugin.Misc.Newsman.Models.Product;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.Newsman.Services
{
    /// <summary>
    /// Represents MailChimp manager
    /// </summary>
    public class NewsmanManager
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly IPictureService _pictureService;
        private readonly IUrlRecordService _urlRecordRepository;
        private readonly IProductService _productService;
        private readonly RESTClient _newsmanRest;

        #endregion

        #region Ctor

        public NewsmanManager(
            ICustomerService customerService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            ISettingService settingService,
            IStoreService storeService,
            IStoreContext storeContext,
            IPictureService pictureService,
            IUrlRecordService urlRecordRepository,
            IProductService productService,
            NewsmanSettings mailChimpSettings)
        {
            _customerService = customerService;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _settingService = settingService;
            _storeService = storeService;
            _storeContext = storeContext;
            _pictureService = pictureService;
            _urlRecordRepository = urlRecordRepository;
            _productService = productService;

            var newsmanSettings = GetSettings();

            _newsmanRest = new RESTClient(newsmanSettings.UserId, newsmanSettings.ApiKey);
        }

        #endregion

        #region Methods

        public NewsmanSettings GetSettings()
        {
            var storeId = _storeContext.GetActiveStoreScopeConfigurationAsync();
            var newsmanSettings = _settingService.LoadSettingAsync<NewsmanSettings>(storeId.Result).Result;

            return newsmanSettings;
        }

        public IList<SelectListItem> GetAvailableListsAsync()
        {
            var response = this._newsmanRest.CallMethod("list", "all", new System.Collections.Specialized.NameValueCollection());

            List<ListsModel> availableLists;

            try
            {
                availableLists = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ListsModel>>(response);
            }
            catch(Exception ex)
            {
                return new List<SelectListItem>() { new SelectListItem { Text = "Error on retrieving list, check User Id and Api Key", Value = "" } };
            }

            return availableLists.Where(x => x.list_type == "newsletter").Select(list => new SelectListItem { Text = list.list_name, Value = list.list_id.ToString() }).ToList();
        }

        public IList<SelectListItem> GetAvailableSegmentsAsync()
        {
            var storeId = _storeContext.GetActiveStoreScopeConfigurationAsync();
            var newsmanSettings = _settingService.LoadSettingAsync<NewsmanSettings>(storeId.Result).Result;

            List<SegmentsModel> availableSegments;

            var param = new System.Collections.Specialized.NameValueCollection();
            param.Add("list_id", newsmanSettings.ListId);
            var response = this._newsmanRest.CallMethod("segment", "all", param);

            try
            {
                availableSegments = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SegmentsModel>>(response);
            }
            catch (Exception ex)
            {
                return new List<SelectListItem>() { new SelectListItem { Text = "No list selected or too many requests for Segment List", Value = "" } };
            }

            return availableSegments.Select(segment => new SelectListItem { Text = segment.segment_name, Value = segment.segment_id.ToString() }).ToList();
        }

        public async Task<string> Synchronize(bool cronLast, int? limit)
        {
            int? start = 1;
            int batchSize = 9000;
            var data = new List<SyncModel>();
            List<SyncModel> tempData = new List<SyncModel>();
            var settings = GetSettings();
            var message = "No subscribers found";

            NameValueCollection param = new NameValueCollection();
            param.Add("list_id", settings.ListId);
            var segment = string.IsNullOrEmpty(settings.SegmentId) ? null : "[" + settings.SegmentId + "]";
            param.Add("segments", segment);

            var subscribers = await _newsLetterSubscriptionService.GetAllNewsLetterSubscriptionsAsync();

            foreach (var _subscribers in subscribers)
            {
                if (!_subscribers.Active)
                    continue;

                data.Add(
                    new SyncModel()
                    {
                        email = _subscribers.Email
                    }
                );
            }

            if (cronLast)
            {
                var _data = data.Count();

                start = _data - limit;

                if (start < 1)
                {
                    start = 1;
                }

                data = data.Skip((int)start).Take((int)limit).ToList();
            }

            foreach (var _data in data)
            {
                tempData.Add(_data);

                if ((tempData.Count() & batchSize) == 0)
                {
                    message = ImportCSV(tempData, param);

                    tempData = new List<SyncModel>();
                }
            }

            if (tempData.Count() > 0)
            {
                message = ImportCSV(tempData, param);
            }

            if (settings.ImportType == "subscribersCustomers")
            {
                var customersData = new List<SyncModel>();
                var customersDataTemp = new List<SyncModel>();

                var customers = await _customerService.GetAllCustomersAsync();

                foreach (var _customers in customers)
                {
                    if (!_customers.Active)
                        continue;

                    customersData.Add(
                        new SyncModel()
                        {
                            email = _customers.Email,
                            firstname = _customers.FirstName,
                            lastname = _customers.LastName
                        }
                    );
                }

                if (cronLast)
                {
                    var _data = customersData.Count();

                    start = _data - limit;

                    if (start < 1)
                    {
                        start = 1;
                    }

                    customersData = customersData.Skip((int)start).Take((int)limit).ToList();
                }

                foreach (var _customersData in customersData)
                {
                    customersDataTemp.Add(_customersData);

                    if ((customersDataTemp.Count() & batchSize) == 0)
                    {
                        message = ImportCSV(customersDataTemp, param);

                        customersDataTemp = new List<SyncModel>();
                    }
                }

                if (customersDataTemp.Count() > 0)
                {
                    message = ImportCSV(customersDataTemp, param);
                }

                message += (customersData.Count() > 0) ? " Customers imported: " + customersData.Count() + " " : " ";
            }

            message += (data.Count() > 0) ? " Subscribers imported: " + data.Count() : "";

            return message;
        }

        public string ImportCSV(List<SyncModel> data, NameValueCollection param)
        {
            param.Remove("csv_data");

            var csv = "\"email\"\n";

            foreach (var _data in data)
            {
                csv += _data.email + Environment.NewLine;
            }

            param.Add("csv_data", csv);

            try
            {
                var response = this._newsmanRest.CallMethod("import", "csv", param);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "Sync successful";
        }

        public string SetFeedList(string list_id, string url, string website)
        {
            var response = "";

            try
            {
                NameValueCollection param = new NameValueCollection();
                param.Add("list_id", list_id);
                param.Add("url", url);
                param.Add("website", website);
                param.Add("type", "NewsMAN API");

                response = this._newsmanRest.CallMethod("feeds", "setFeedOnList", param);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return response;
        }

        public async Task<Product> GetOrderItems(OrderItem item, string host)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);

            string imageUrl = "";
            var picture = await _pictureService.GetPicturesByProductIdAsync(product.Id);
            if (picture != null || picture.Count > 0)
                imageUrl = await _pictureService.GetPictureUrlAsync(picture.FirstOrDefault().Id);

            var url = await _urlRecordRepository.GetSeNameAsync(product.Id, "Product");
            url = "https://" + host + "/" + url;

            return new Product()
            {
                id = product.Id,
                name = product.Name,
                stock_quantity = product.StockQuantity,
                price = product.Price,
                price_old = product.OldPrice,
                image_url = imageUrl,
                url = url
            };
        }

        #endregion
    }
}