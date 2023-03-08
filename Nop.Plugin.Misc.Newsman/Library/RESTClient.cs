using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace NET_Newsman_API_Wrapper
{
    public class RESTClient
    {
        public string UserId { get; set; }
        public string ApiKey { get; set; }
        public string REST_Url { get; set; }

        public RESTClient(string userId, string apiKey)
        {
            this.UserId = userId;
            this.ApiKey = apiKey;
            this.REST_Url = "https://ssl.newsman.ro/api/1.2/rest/" + userId + "/" + apiKey + "/";
        }

        public string CallMethod(string _namespace, string method, NameValueCollection paramCollection)
        {
            var response = "";

            try
            {
                using (var client = new WebClient())
                {
                    var _response = client.UploadValues(this.REST_Url + _namespace + "." + method + ".json", paramCollection);

                    response = Encoding.Default.GetString(_response);
                }
            }
            catch (WebException ex)
            {
                response = ex.Message;
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }

            return response;
        }
    }
}