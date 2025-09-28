using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using WmsSystem.Data;
using WmsSystem.Models;
using Newtonsoft.Json;

namespace WmsSystem.Filters
{
    public class AuditLogAttribute : ActionFilterAttribute
    {
        private Stopwatch _stopwatch;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            _stopwatch = Stopwatch.StartNew();
            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            _stopwatch.Stop();
            
            try
            {
                LogAction(filterContext);
            }
            catch
            {
                // Don't let logging errors break the application
            }
            
            base.OnActionExecuted(filterContext);
        }

        private void LogAction(ActionExecutedContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var user = filterContext.HttpContext.User;
            
            var log = new SystemLog
            {
                Ts = DateTime.Now,
                UserName = user?.Identity?.Name ?? "Anonymous",
                Ip = GetClientIpAddress(request),
                Controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName,
                Action = filterContext.ActionDescriptor.ActionName,
                HttpMethod = request.HttpMethod,
                Url = request.Url?.ToString(),
                Params = SerializeParameters(filterContext.ActionParameters),
                Result = filterContext.Exception == null ? "Success" : "Error",
                DurationMs = (int)_stopwatch.ElapsedMilliseconds
            };

            // Save to database in background
            System.Threading.Tasks.Task.Run(() => SaveLog(log));
        }

        private string GetClientIpAddress(HttpRequestBase request)
        {
            var ipAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            
            if (!string.IsNullOrEmpty(ipAddress))
            {
                var addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                    return addresses[0];
            }
            
            return request.ServerVariables["REMOTE_ADDR"];
        }

        private string SerializeParameters(IDictionary<string, object> parameters)
        {
            try
            {
                // Mask sensitive parameters
                var maskedParams = new Dictionary<string, object>();
                foreach (var param in parameters)
                {
                    if (IsSensitiveParameter(param.Key))
                        maskedParams[param.Key] = "***MASKED***";
                    else
                        maskedParams[param.Key] = param.Value;
                }
                
                return JsonConvert.SerializeObject(maskedParams);
            }
            catch
            {
                return "Error serializing parameters";
            }
        }

        private bool IsSensitiveParameter(string paramName)
        {
            var sensitiveParams = new[] { "password", "token", "secret", "key" };
            return Array.Exists(sensitiveParams, s => paramName.ToLower().Contains(s));
        }

        private void SaveLog(SystemLog log)
        {
            try
            {
                using (var context = new WmsDbContext())
                {
                    context.SystemLogs.Add(log);
                    context.SaveChanges();
                }
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
