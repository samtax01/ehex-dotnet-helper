using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Ehex.Helpers
{

    /// <summary>
    /// @version: 1.0
    /// @by: Samson Oyetola [hello@samsonoyetola.com]
    /// @repo: 
    /// </summary>
    
    public static class ApiException
    {
        
        
        /// <summary>
        /// Handle Exception at global level.
        /// Add to Startup Configuration as
        /// <br/> app.UseApiExceptionHandler();
        /// </summary>
        /// <param name="app"></param>
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
                        await context.Response.WriteAsync(new ApiResponse<object>(){ Message = contextFeature.Error.Message, Status = false}.ToString() );
                    }
                });
            });
        }
    }
}