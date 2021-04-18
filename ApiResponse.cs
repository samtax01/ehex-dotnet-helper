using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;




// ReSharper disable once CheckNamespace
namespace Ehex.Helpers
{
    // Helper Class
    // ApiResponse serves as a response wrapper to guarantee a consistent Api response across the Application
    // @version: 1.0
    // @creator: Samson Oyetola [hello@samsonoyetola.com]
    // @repo: https://github.com/samtax01/ehex-dotnet-helper
    // use case
    //      Add to swagger documentation annotation as
    //          [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    // 
    //      Return in a controller of IActionResult as
    //          Status(StatusCodes.Status202Accepted, ApiResponse<string>.Create("My Data", "Action Successful", 200));
    //
    
    
    /// <summary>
    /// Standard API Response
    /// <br/>Status: is response successful or not
    /// <br/>Message: Friendly Comment on the response
    /// <br/>Data: Response data
    /// </summary>
    public class ApiResponse<T>: ActionResult
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// API response status is a quick True or False value that determines if the API request is successful or not.
        /// </summary>
        /// <example>True</example>
        public bool Status { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// API response message is a user friendly status message that describes the API response.
        /// </summary>
        /// <example>Success</example>
        public string Message { get; set; }
        
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// Response content for the API. 
        /// </summary>
        public T Data { get; set; }
        

        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// Create and Return full response information
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ObjectResult Create([ActionResultObjectValue] T data = default,  string message = "", [ActionResultStatusCode] int statusCode = 0)
        {
            return new (new ApiResponse<T>
            {
                Status = !(statusCode > 299),
                Message = message,
                Data = data,
            }) {
                StatusCode = statusCode
            };
        }
        
        
        /// <summary>
        /// Return A Successful response message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static ObjectResult SuccessMessage(string message = "success", int statusCode = 200)
        {
            return Create(default, message, statusCode);
        }
        
        
        
        /// <summary>
        /// Return A Failed response message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static ObjectResult FailureMessage(string message = "Failed", int statusCode = 500)
        {
            return Create(default, message, statusCode);
        }
        
        
        /// <summary>
        /// Return A Successful Data and Message
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ObjectResult Success(T data = default, int statusCode = 200)
        {
            return Create(data, "success", statusCode);
        }


        /// <summary>
        /// Extract an ApiResponse content from an the HttpResponseMessage object
        /// <br/>
        ///  Sample Usage:
        ///     var responseMessage = await HttpRequest.Make("https://localhost:5001/api/v1/pingin.com/2");
        ///     var jsonResponse = await ApiResponse<ExpandoObject/>.FromRequest(responseMessage);
        ///     Console.Write(apiResponse);
        ///
        /// <br/>
        /// Accessing a field that does not exists will throw RuntimeBinderException. Otherwise, use dynamic e.g ApiResponse<dynamic/>.FromRequest(responseMessage) to suppress error
        /// try {
        ///     var value = jsonResponse.FieldThatDoesntExist;
        /// }
        /// catch(RuntimeBinderException) { ... }
        /// </summary>
        /// <remarks>
        ///    
        /// </remarks>>
        /// <param name="responseMessage"></param>
        /// <param name="forceThrowError"></param>
        /// <returns></returns>
        public static async Task<ApiResponse<T>> FromRequest(HttpResponseMessage responseMessage, bool forceThrowError = false) 
        {
            static bool IsApiResponseValid(ApiResponse<T> apiResponse) => !((apiResponse == null) || (apiResponse.Status == false && apiResponse.Message == null && apiResponse.Data == null));
            var response = new ApiResponse<T>
            {
                Status = responseMessage.IsSuccessStatusCode,
                Message = responseMessage.IsSuccessStatusCode? "success": "failed. " + responseMessage.StatusCode.GetDisplayName()
            };
            
            try
            {
                // fetch as string
                var responseAsString = await responseMessage.Content.ReadAsStringAsync();
                //var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};

                if(forceThrowError)  
                    responseMessage.EnsureSuccessStatusCode();
                
                //Try convert to ApiResponse<T> Standard response
                //var response0 = JsonSerializer.Deserialize<ApiResponse<T>>(responseAsString, options);
                var response0 = JsonConvert.DeserializeObject<ApiResponse<T>>(responseAsString);
                if (IsApiResponseValid(response0))
                    return response0;
                
                // Force Convert to ApiResponse<T>
                //response.Data = JsonSerializer.Deserialize<T>(responseAsString, options)?? (T) (object) responseAsString;
                response.Data = JsonConvert.DeserializeObject<T>(responseAsString)?? (T) (object) responseAsString;
            }
            catch (Exception e)
            {
                response.Status = false;
                response.Message = $"Parse Error. Unable to convert to dataType {typeof(T)}. Use dynamic/ExpandoObject dataType instead";
                Console.WriteLine(e);
                if(forceThrowError) throw;
            }
            return response;
        }

        
        
        
        
        /// <summary>
        /// Convert to nice string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                //Formatting = Formatting.Indented
            });
        }

        
    }
}