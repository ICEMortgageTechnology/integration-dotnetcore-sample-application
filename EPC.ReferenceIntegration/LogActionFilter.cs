using Microsoft.AspNetCore.Http;
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

                using (var bodyReader = new StreamReader(context.Request.Body))
                {
                    string body = await bodyReader.ReadToEndAsync();
                    
                    //_logger.LogInformation("Request ---[START]------------------ ");
                    //_logger.LogInformation(body);
                    //_logger.LogInformation("Request ---[END]------------------ ");
                }

                //Need to stop the timer after the pipeline finish
                timer.Stop();
                _logger.LogDebug("The request took '{0}' ms", timer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
