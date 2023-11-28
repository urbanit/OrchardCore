using System;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OrchardCore.DisplayManagement.Layout;

namespace OrchardCore.DisplayManagement.Notify
{
    public class NotifyFilter : IActionFilter, IAsyncResultFilter, IPageFilter
    {
        public const string CookiePrefix = "orch_notify";
        private readonly INotifier _notifier;
        private readonly IShapeFactory _shapeFactory;
        private readonly ILayoutAccessor _layoutAccessor;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        private NotifyEntry[] _existingEntries = Array.Empty<NotifyEntry>();
        private bool _shouldDeleteCookie;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger _logger;

        public NotifyFilter(
            INotifier notifier,
            ILayoutAccessor layoutAccessor,
            IShapeFactory shapeFactory,
            IDataProtectionProvider dataProtectionProvider,
            HtmlEncoder htmlEncoder,
            ILogger<NotifyFilter> logger)
        {
            _htmlEncoder = htmlEncoder;
            _logger = logger;
            _dataProtectionProvider = dataProtectionProvider;

            _layoutAccessor = layoutAccessor;
            _notifier = notifier;
            _shapeFactory = shapeFactory;
        }

        private void OnHandlerExecuting(FilterContext filterContext)
        {
            var messages = Convert.ToString(filterContext.HttpContext.Request.Cookies[CookiePrefix]);
            if (string.IsNullOrEmpty(messages))
            {
                return;
            }

            DeserializeNotifyEntries(messages, out var messageEntries);

            if (messageEntries == null)
            {
                // An error occurred during deserialization.
                _shouldDeleteCookie = true;
                return;
            }

            if (messageEntries.Length == 0)
            {
                return;
            }

            // Make the notifications available for the rest of the current request.
            _existingEntries = messageEntries;
        }

        private void OnHandlerExecuted(FilterContext filterContext)
        {
            var messageEntries = _notifier.List().ToArray();

            // Don't touch temp data if there's no work to perform.
            if (messageEntries.Length == 0 && _existingEntries.Length == 0)
            {
                return;
            }

            // Assign values to the Items collection instead of TempData and
            // combine any existing entries added by the previous request with new ones.

            _existingEntries = messageEntries.Concat(_existingEntries).Distinct(new NotifyEntryComparer(_htmlEncoder)).ToArray();
            object result = filterContext is ActionExecutedContext ace ? ace.Result : ((PageHandlerExecutedContext)filterContext).Result;
            // Result is not a view, so assume a redirect and assign values to TemData.
            // String data type used instead of complex array to be session-friendly.
            if (result is not ViewResult && result is not PageResult && _existingEntries.Length > 0)
            {
                filterContext.HttpContext.Response.Cookies.Append(CookiePrefix, SerializeNotifyEntry(_existingEntries), GetCookieOptions(filterContext.HttpContext));
            }
        }

        #region Interface wrappers

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            OnHandlerExecuting(filterContext);
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            OnHandlerExecuted(filterContext);
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext filterContext)
        {
            OnHandlerExecuting(filterContext);
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
            OnHandlerExecuted(context);
        }

        #endregion

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            if (_shouldDeleteCookie)
            {
                DeleteCookies(filterContext);

                await next();
                return;
            }

            if (!filterContext.IsViewOrPageResult())
            {
                await next();
                return;
            }

            if (_existingEntries.Length == 0)
            {
                await next();
                return;
            }

            var layout = await _layoutAccessor.GetLayoutAsync();

            var messagesZone = layout.Zones["Messages"];

            if (messagesZone is IShape zone)
            {
                foreach (var messageEntry in _existingEntries)
                {
                    // Also retrieve the actual zone in case it was only a temporary empty zone created on demand.
                    zone = await zone.AddAsync(await _shapeFactory.CreateAsync("Message", Arguments.From(messageEntry)));
                }
            }

            DeleteCookies(filterContext);

            await next();
        }

        private static void DeleteCookies(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Cookies.Delete(CookiePrefix, GetCookieOptions(filterContext.HttpContext));
        }

        private string SerializeNotifyEntry(NotifyEntry[] notifyEntries)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new NotifyEntryConverter(_htmlEncoder));

            try
            {
                var protector = _dataProtectionProvider.CreateProtector(nameof(NotifyFilter));
                var signed = protector.Protect(JsonConvert.SerializeObject(notifyEntries, settings));
                return WebUtility.UrlEncode(signed);
            }
            catch
            {
                return null;
            }
        }

        private void DeserializeNotifyEntries(string value, out NotifyEntry[] messageEntries)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new NotifyEntryConverter(_htmlEncoder));

            try
            {
                var protector = _dataProtectionProvider.CreateProtector(nameof(NotifyFilter));
                var decoded = protector.Unprotect(WebUtility.UrlDecode(value));
                messageEntries = JsonConvert.DeserializeObject<NotifyEntry[]>(decoded, settings);
            }
            catch
            {
                messageEntries = null;

                _logger.LogWarning("The notification entries could not be decrypted");
            }
        }

        private static CookieOptions GetCookieOptions(HttpContext httpContext)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true
            };

            if (httpContext.Request.PathBase.HasValue)
            {
                cookieOptions.Path = httpContext.Request.PathBase;
            }

            return cookieOptions;
        }
    }
}
