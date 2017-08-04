using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }


        public async Task Invoke(HttpContext context)
        {
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                await _next(context);

                _logger.LogInformation("[LogActionFilter] - Request ---[START]----------------------------------------------------------------------------------------------------------------------------- ");
                _logger.LogInformation("[LogActionFilter] - Request Host - " + context.Request.Host.Host);
                _logger.LogInformation("[LogActionFilter] - Request Path - " + context.Request.Path);
                _logger.LogInformation("[LogActionFilter] - Request QueryString - " + context.Request.QueryString.ToString());
                _logger.LogInformation("[LogActionFilter] - Request Remote IP Address - " + context.Connection.RemoteIpAddress.MapToIPv4().ToString());
                foreach (var item in context.Request.Headers)
                {
                    _logger.LogInformation(string.Format("[LogActionFilter] - Request Header - [{0} - {1}] ", item.Key, item.Value));
                }
                
                _logger.LogInformation("[LogActionFilter] - Elapsed MilliSeconds - " + timer.ElapsedMilliseconds);
                
                _logger.LogInformation("[LogActionFilter] - Request ---[END]------------------------------------------------------------------------------------------------------------------------------- ");

                _logger.LogInformation("[LogActionFilter] - Response ---[START]----------------------------------------------------------------------------------------------------------------------------- ");
                _logger.LogInformation("[LogActionFilter] - Response Content Length - " + context.Response.ContentLength);
                _logger.LogInformation("[LogActionFilter] - Response Content Type - " + context.Response.ContentType);
                foreach (var item in context.Response.Headers.ToDictionary(l => l.Key, k => k.Value))
                {
                    _logger.LogInformation(string.Format("[LogActionFilter] - Response Header - [{0} - {1}] ", item.Key, item.Value));
                }
                
                _logger.LogInformation("[LogActionFilter] - Response ---[END]------------------------------------------------------------------------------------------------------------------------------- ");


                //Need to stop the timer after the pipeline finish
                timer.Stop();
                _logger.LogDebug("[LogActionFilter] - The request took '{0}' ms", timer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            var body = request.Body;
            request.EnableRewind();

            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body = body;

            return $"{request.Scheme} {request.Host}{request.Path} {request.QueryString} {bodyAsText}";
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return $"Response {text}";
        }
    }

    public static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
