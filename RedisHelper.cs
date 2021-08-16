using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

// ReSharper disable once CheckNamespace
#pragma warning disable 1570
namespace Ehex.Helpers
{
    
    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    ///
    ///     "ConnectionStrings": {
    ///         "alatpay_redis" : "127.0.0.1:5555"
    ///     },
    ///
    ///
    ///     // Add Redis Cache
    ///     services.AddStackExchangeRedisCache(options =>
    ///     {
    ///         options.Configuration = Configuration.GetConnectionString("mydemo_redis");
    ///         options.InstanceName = "MyDemo_App";
    ///     });
    /// 
    /// </summary>
    public class RedisHelper
    {

        /// <summary> Insert </summary>
        public static async Task InsertAsync<T>(string key, T value, IDistributedCache cache, TimeSpan? expiredAt = null, TimeSpan? unusedExpiredAy = null)
        {
            var option = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiredAt ?? TimeSpan.FromMinutes(5),
                SlidingExpiration = unusedExpiredAy ?? TimeSpan.FromHours(1)
            };
            
            var content = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, content, option);
        }
        
        
        /// <summary> Get </summary>
        public static async Task<T> GetAsync<T>(string key, IDistributedCache cache)
        {
            var content = await cache.GetStringAsync(key);
            return content is null? default: JsonSerializer.Deserialize<T>(content);
        }
    }
    
    
}