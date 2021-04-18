using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Ehex.Helpers
{

    public enum HttpRequestType
    {
        Get,
        Post,
        Put,
    }
    
    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @creator: Samson Oyetola [hello@samsonoyetola.com]
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    /// </summary>
    public static class HttpRequest
    {
        private static readonly HttpClient Client = new();
        

        /// <summary>
        ///     Add Basic Authentication to a request header.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="httpClient">optional as HttpService.Client</param>
        /// <returns></returns>
        public static HttpClient AddBasicAuth(string userName, string password, HttpClient httpClient = null)
        {
            httpClient ??= Client;
            var authToken = Encoding.ASCII.GetBytes($"{userName}:{password}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
            return httpClient;
        }


        /// <summary>
        /// Make a request and get a string response.
        /// This string can later be converted to JSON of an object...
        /// <br/>
        ///  Sample Usage:
        ///     var responseMessage = await HttpRequest.Make("https://localhost:5001/api/v1/pingin.com/2");
        ///     var apiResponse = await ApiResponse<dynamic/>.FromRequest(responseMessage);
        ///     Console.Write(apiResponse);
        /// <br/> OR
        /// <br/>- Convert to ApiResponse ApiResponse<dynamic/>.FromRequest(result)
        /// <br/>- Get string value with "response.Content.ReadAsStringAsync()"
        /// <br/>- Use JsonConvert.DeserializeObject<dynamic/>( response.Content ) to convert to a dynamic object.
        /// <br/>- Use ApiResponse<dynamic/>.FromRequest(response) to normalize value.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestType">e.g HttpRequestType requestType = HttpRequestType.GET</param>
        /// <param name="requestData"></param>
        /// <param name="httpClient">optional as HttpService.Client</param>
        /// <returns>Returned HttpResponseMessage</returns>
        public static async Task<HttpResponseMessage> Make(string url, HttpRequestType requestType = HttpRequestType.Get, HttpContent requestData = null, HttpClient httpClient = null)
        {
            try
            {
                httpClient ??= Client;
                var response = requestType switch
                {
                    HttpRequestType.Post => (await httpClient.PostAsync(url, requestData!)),
                    HttpRequestType.Put => (await httpClient.PutAsync(url, requestData!)),
                    _ => httpClient.GetAsync(url).Result
                };
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Occurred while making request to {0} - Detail: {1}", url, e);
                throw;
            }
        }
        
        
        
        
        
    }
}