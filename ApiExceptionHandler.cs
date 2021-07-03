using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
#pragma warning disable 1570
namespace Ehex.Helpers
{

    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    /// </summary>
    public static class ApiExceptionHandler
    {
        
        
        /// <summary>
        /// Handle Exception at global level.
        /// Create to Startup Configuration as
        /// <br/> app.UseApiExceptionHandler();
        /// </summary>
        public static void UseApiExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if(contextFeature != null)
                    {
                        context.Response.StatusCode = contextFeature.Error switch
                        {
                            ApiException exception => exception.StatusCode,
                            ArgumentException => 422,
                            _ => 500
                        };

                        await context.Response.WriteAsync(new ApiResponse<object>{ Message = contextFeature.Error.Message, Status = false}.ToString() );
                    }
                });
            });
        }
    }

    public class ApiException: Exception
    {
        public ApiException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
        public int StatusCode { set; get; }
    }
}