using Nop.Core.Caching;

namespace Nop.Plugin.Misc.Newsman
{
    /// <summary>
    /// Represents MailChimp plugin constants
    /// </summary>
    public class NewsmanDefaults
    {
        /// <summary>
        /// Plugin system name
        /// </summary>
        public static string SystemName => "Misc.Newsman";

        /// <summary>
        /// Cache key to store the operation number of a synchronization
        /// </summary>
        public static CacheKey OperationNumberCacheKey => new("Newsman-synchronization-operations");

        /// <summary>
        /// Cache key to store handled batches of a synchronization
        /// </summary>
        public static CacheKey SynchronizationBatchesCacheKey => new("Newsman-synchronization-batches");

        /// <summary>
        /// Default mask of store identifier that uniquely identifying the store in MailChimp E-Commerce
        /// </summary>
        /// <remarks>
        /// {0} : Store identifier
        /// </remarks>
        public static string DefaultStoreIdMask => "nopCommerce-store-{0}";

        /// <summary>
        /// Name of the route to the batch webhook handler
        /// </summary>
        public static string BatchWebhookRoute => "Plugin.Misc.Newsman.BatchWebhook";

        /// <summary>
        /// Name of the route to the webhook handler
        /// </summary>
        public static string WebhookRoute => "Plugin.Misc.Newsman.Webhook";

        /// <summary>
        /// An HTTP PATCH protocol method
        /// </summary>
        public static string PatchRequestMethod => "PATCH";

        /// <summary>
        /// An HTTP DELETE protocol method
        /// </summary>
        public static string DeleteRequestMethod => "DELETE";

        /// <summary>
        /// Merge field of a subscription member that contains a first name
        /// </summary>
        public static string FirstNameMergeField => "FNAME";

        /// <summary>
        /// Merge field of a subscription member that contains a last name
        /// </summary>
        public static string LastNameMergeField => "LNAME";

        /// <summary>
        /// Path of API request to manage subscription members 
        /// </summary>
        /// {0} : List identifier
        /// {1} : Email hash
        /// </remarks>
        public static string MembersApiPath => "/lists/{0}/members/{1}";

        /// <summary>
        /// Path of API request to manage stores 
        /// </summary>
        /// {0} : Store identifier
        /// </remarks>
        public static string StoresApiPath => "/ecommerce/stores/{0}";

        /// <summary>
        /// Path of API request to manage customers 
        /// </summary>
        /// {0} : Store identifier
        /// {1} : Customer identifier
        /// </remarks>
        public static string CustomersApiPath => "/ecommerce/stores/{0}/customers/{1}";

        /// <summary>
        /// Path of API request to manage products 
        /// </summary>
        /// {0} : Store identifier
        /// {1} : Product identifier
        /// </remarks>
        public static string ProductsApiPath => "/ecommerce/stores/{0}/products/{1}";

        /// <summary>
        /// Path of API request to manage product variants
        /// </summary>
        /// {0} : Store identifier
        /// {1} : Product identifier
        /// {2} : Product variant identifier
        /// </remarks>
        public static string ProductVariantsApiPath => "/ecommerce/stores/{0}/products/{1}/variants/{2}";

        /// <summary>
        /// Path of API request to manage orders 
        /// </summary>
        /// {0} : Store identifier
        /// {1} : Order identifier
        /// </remarks>
        public static string OrdersApiPath => "/ecommerce/stores/{0}/orders/{1}";

        /// <summary>
        /// Path of API request to manage carts 
        /// </summary>
        /// {0} : Store identifier
        /// {1} : Cart identifier
        /// </remarks>
        public static string CartsApiPath => "/ecommerce/stores/{0}/carts/{1}";

        /// <summary>
        /// Name of the synchronization task
        /// </summary>
        public static string SynchronizationTaskName => "Synchronization with Newsman";

        /// <summary>
        /// Type of the synchronization task
        /// </summary>
        public static string SynchronizationTask => "Nop.Plugin.Misc.Newsman.Services.SynchronizationTask";

        /// <summary>
        /// Default synchronization period in hours
        /// </summary>
        public static int DefaultSynchronizationPeriod => 6;

        /// <summary>
        /// Default batch operation number
        /// </summary>
        public static int DefaultBatchOperationNumber => 2000;
    }
}