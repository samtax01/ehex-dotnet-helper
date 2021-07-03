using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;


// ReSharper disable once CheckNamespace
#pragma warning disable 1570
namespace Ehex.Helpers
{
    
    
    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    /// 
    ///
    /// Add pagination to you list rePagination
    ///    Add to Service
    ///      services.AddHttpContextAccessor();
    ///   
    ///    Usage for IQueryable (Recommended)
    ///       public async Task<IActionResult> GetUsers([FromQuery] PaginationFilter paginationFilter)
    ///   
    ///           var data = await _repo.GetRecords();
    ///           return (await Pagination<Users>.CreateAsync(data, paginationFilter, HttpContext)).ToApiResponse();
    ///   
    ///       }
    ///   
    ///   
    ///    Usage for Ilist
    ///      var result = new List();
    ///      Pagination<Settlement>.Create(result, paginationFilter, HttpContext).ToApiResponse()
    /// 
    /// </summary>
    public class Pagination<T>
    {


        /// <summary>
        /// List of data
        /// </summary>
        public IList<T> Items { get; set; }
        
        /// <summary>
        /// Current Page
        /// </summary>
        public int CurrentPage { get; set; }
        
        /// <summary>
        /// Total Items
        /// </summary>
        public int TotalItems { get; set; }
        
        /// <summary>
        /// Page Pages
        /// </summary>
        public int TotalPages { get; set; }
        
        /// <summary>
        /// Current Request Url
        /// </summary>
        public Uri PathUrl { get; set; }
        
        /// <summary>
        /// Previous Url
        /// </summary>
        public Uri PreviousPageUrl { get; set; }
        
        /// <summary>
        /// Next Url
        /// </summary>
        public Uri NextPageUrl { get; set; }

        private Pagination(){}

        private static Pagination<T> Build(IList<T> pagedItems, int totalItems, PaginationFilter paginationFilter, HttpContext request)
        {
            var totalPages = (int) Math.Ceiling(totalItems / (double) paginationFilter.Limit);
            
            // Page Url. Get Url with (SSL offloading)
            string scheme, baseUrl, path = "";
            if (request is not null)
            {
                scheme = request.Request.Host.Host.Contains("localhost") ? request.Request.Scheme : "https";
                baseUrl = $"{scheme}://{request.Request.Host}{request.Request.PathBase}";
                path = baseUrl + request.Request.Path;
            }

            return new Pagination<T>
            {
                Items = pagedItems,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = paginationFilter.Page,
                
                PathUrl =  new Uri(path),
                PreviousPageUrl = paginationFilter.Page > 1? new Uri($"{path}?limit={paginationFilter.Limit}&page={paginationFilter.Page - 1}"): null,
                NextPageUrl =  totalPages > paginationFilter.Page ? new Uri($"{path}?limit={paginationFilter.Limit}&page={paginationFilter.Page + 1}"): null,
            };
        }
        
        /// <summary>
        /// Create Pagination Information for Items
        /// [ Recommended ]
        /// </summary>
        /// <param name="request"></param>
        /// <param name="items"></param>
        /// <param name="paginationFilter"></param>
        /// <returns></returns>
        public static async Task<Pagination<T>> CreateAsync([ActionResultObjectValue] IQueryable<T> items, PaginationFilter paginationFilter, HttpContext request)
        {
            var pagedItems =   await items.Skip((paginationFilter.Page - 1) * paginationFilter.Limit).Take(paginationFilter.Limit).ToListAsync();
            var totalItems = await items.CountAsync();
            return Build(pagedItems, totalItems, paginationFilter, request);
        }
        
        /// <summary>
        /// Create Pagination Information for Items
        /// </summary>
        /// <param name="request"></param>
        /// <param name="items"></param>
        /// <param name="paginationFilter"></param>
        /// <returns></returns>
        public static Pagination<T> Create([ActionResultObjectValue] IList<T> items, PaginationFilter paginationFilter, HttpContext request)
        {
            var pagedItems = items.Skip((paginationFilter.Page - 1) * paginationFilter.Limit).Take(paginationFilter.Limit).ToList();
            return Build(pagedItems, items.Count, paginationFilter, request);
        }


        /// <summary>
        /// Convert Response to ApiResonse
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public ObjectResult ToApiResponse(string message = null, [ActionResultStatusCode] int statusCode = StatusCodes.Status200OK)
        {
            message ??= $"{Items.Count} row(s) returned";
            return ApiResponse<Pagination<T>>.Create(this, message, statusCode);
        }
        
    }
    
    
    /// <summary>
    /// Optional Pagination Filter
    /// </summary>
    public class PaginationFilter
    {
        /// <summary>
        /// Items Page number
        /// </summary>
        /// <example>1</example>
        public int Page { get; set; }
        
        /// <summary>
        /// Items to display Per Page
        /// </summary>
        /// <example>10</example>
        public int Limit { get; set; }
        
        
        public PaginationFilter()
        {
            Page = 1;
            Limit = 10;
        }
        
        public PaginationFilter(int page = 1, int limit = 10)
        {
            Page = page < 1 ? 1 : page;
            Limit = limit;
        }
    }


}