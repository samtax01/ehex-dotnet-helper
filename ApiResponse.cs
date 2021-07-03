using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;




// ReSharper disable once CheckNamespace
#pragma warning disable 1570
namespace Ehex.Helpers
{
    
    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    ///
    /// ApiResponse serves as a response wrapper to guarantee a consistent API response across the Application
    /// 
    /// use case
    ///     Swagger documentation annotation for Controller as
    ///     [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    /// 
    /// Return in a controller of IActionResult as
    ///     return ApiResponse<string>.Create("request data", "Action Successful", 200);
    ///
    /// Standard API Response
    ///     <br/>Status: bool, is response successful or not
    ///     <br/>Message: string, friendly feedback notification message
    ///     <br/>Data: object, Response payload
    /// </summary>
    public abstract class ApiResponse: ActionResult
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
        /// Create and Return full response information
        /// </summary>
        public static ObjectResult Create([ActionResultObjectValue] object data = default,  string message = "", [ActionResultStatusCode] int statusCode = 0)
        {
            return new (new ApiResponse<object>
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
        public static ObjectResult SuccessMessage(string message = "Success", int statusCode = 200)
        {
            return Create(null, message, statusCode);
        }
        
        
        
        /// <summary>
        /// Return A Failed response message
        /// </summary>
        public static ObjectResult FailureMessage(string message = "Failed", int statusCode = 500)
        {
            return Create(null, message, statusCode);
        }
        
        
        /// <summary>
        /// Convert to nice string
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
            });
        }
    }
    
    
    /// <summary>
    /// Generic Version
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResponse<T>: ApiResponse
    {
        
        
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// Response content for the API. 
        /// </summary>
        public T Data { get; set; }
        

        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// Create and Return full response information
        /// </summary>
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
        /// Return A Successful Data and Message
        /// </summary>
        public static ObjectResult Success(T data = default, int statusCode = 200)
        {
            return Create(data, "Success", statusCode);
        }


        /// <summary>
        /// Extract an ApiResponse content from an the HttpResponseMessage object.
        /// 
        /// In case the requested api response doesn't  follow our ApiResponse, It would be converted
        /// to ApiResponse automatically and The  ApiResponse.Status would be determined by the api request HttpStatusCode.
        /// <br/>
        ///  Sample Usage:
        ///     var responseMessage = await HttpRequest.MakeAsync("https://localhost:5001/api/v1/plugins/2");
        ///     var jsonResponse = await ApiResponse<string>.FromRequestAsync(responseMessage);
        ///     Console.Write(apiResponse);
        /// 
        /// <br/>
        /// if forceThrowError param is true, Accessing a field that does not exists will throw Exception. Otherwise, use dynamic e.g ApiResponse<dynamic/>.FromRequest(responseMessage) to suppress error
        /// </summary>
        public static async Task<ApiResponse<T>> FromRequestAsync(HttpResponseMessage responseMessage, bool forceThrowError = false)
        {
            var responseString = "";
            static bool IsApiResponseValid(ApiResponse<T> apiResponse) => !((apiResponse == null) || (apiResponse.Status == default && apiResponse.Message == default && apiResponse.Data.Equals(default)));
            var response = new ApiResponse<T>
            {
                Status = responseMessage.IsSuccessStatusCode,
                Message = responseMessage.IsSuccessStatusCode? "Success": "Failed with httpStatus " + responseMessage.StatusCode.GetDisplayName()
            };
            
            try
            {
                // fetch as string
                responseString = await responseMessage.Content.ReadAsStringAsync();
                if (typeof(T) == typeof(string))
                {
                    response.Data = (T)(object)responseString;
                    return response;
                }
                
                
                if(forceThrowError)  
                    responseMessage.EnsureSuccessStatusCode();
                
                try
                {
                    // When response is a real ApiResponse response
                    var responseIsReal = JsonConvert.DeserializeObject<ApiResponse<T>>(responseString);
                    if (!IsApiResponseValid(responseIsReal))
                        throw new Exception($"ApiResponse contract not match with object {typeof(T).Name}.");
                    return responseIsReal;
                }
                catch (Exception)
                {
                    if (forceThrowError) throw;
                    try
                    {
                        // Force convert unknown response to ApiResponse<T>
                        response.Data = JsonConvert.DeserializeObject<T>(responseString)?? (T) (object) responseString;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Bad Response. Unable to determine json object. {ex.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                response.Status = false;
                response.Message = $"ApiResponse.FromRequest() Parse Error. Unable to convert to dataType {typeof(T).Name}. Try string/dynamic dataType instead. Raw Response: {responseString}";
                Console.WriteLine(e);
                if(forceThrowError) throw;
            }
            return response;
        }
    }
    
    
}