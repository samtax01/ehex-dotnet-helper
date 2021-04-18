using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable once CheckNamespace
namespace Ehex.Helpers
{
  
    public interface IQueueService
    {
        Task SendMessage<T>(T messageObject, string url, string entityPath);
        void ReceiveMessage<T>(Func<T, Task> callBackMethod, string url, string entityPath);
        
    }
    
    
    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @creator: Samson Oyetola [hello@samsonoyetola.com]
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    /// Usage
    /// <br/>
    /// Add to Startup
    ///     services.AddScoped[IQueueService, QueueService](); 
    /// </summary>
    public class QueueService : IQueueService
    {
        
        private readonly ILogger<QueueService> _logger;
        private readonly IDictionary<string, QueueClient> _queueClients = new Dictionary<string, QueueClient>();

        public QueueService(ILogger<QueueService> logger)
        {
            _logger = logger;
        }
        

        public async Task SendMessage<T>(T messageObject, string url, string entityPath) 
        {
            try
            {
                // Create Queue
                if(!_queueClients.ContainsKey(url+entityPath))
                    _queueClients.Add(url+entityPath, new QueueClient(url, entityPath));
                var queueClient = _queueClients[url+entityPath];
                var messageContent = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageObject));
                var message = new Message(messageContent);
                //message.UserProperties["MessageType"] = "";
                await queueClient.SendAsync(message);
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to send to [{0}] queue. Reason: {1}", entityPath, e.Message);
            }
        }
        
        

        /// <summary>
        /// Listen for a queue event
        /// Add to Application Startup part where a listener can be registered.
        /// <br/> If you get error "Cannot access a disposed context instance.". Simply add ServiceLifetime.Singleton
        ///  <br/>  e.g services.AddDbContext<DatabaseContext/>(options => options.UseSqlServer( Configuration.GetConnectionString("ms_db") ), ServiceLifetime.Singleton);
        /// </summary>
        /// <param name="callBackMethod"></param>
        /// <param name="url"></param>
        /// <param name="entityPath"></param>
        /// <typeparam name="T"></typeparam>
        public  void ReceiveMessage<T>(Func<T, Task> callBackMethod, string url, string entityPath)
        {
            try
            {
                // Create Queue
                if(!_queueClients.ContainsKey(url+entityPath))
                    _queueClients.Add(url+entityPath, new QueueClient(url, entityPath));
                var queueClient = _queueClients[url+entityPath];
            
                // Message Handler
                var messageHandler = new MessageHandlerOptions(
                    e => {
                        _logger.LogError("Unable to read data from queue [{0}]. Reason: {1}", entityPath, e.Exception.Message);
                        return Task.CompletedTask;
                    }
                )
                {
                    MaxConcurrentCalls = 1, // Total Item to run at a time.
                    AutoComplete = false, // Delay until after Queue TTL. (30s)
                };

                // Callback method
                queueClient.RegisterMessageHandler(async (message, cancellationToken) =>
                {
                    try
                    {
                        var json = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
                        await callBackMethod(json);
                        _logger.LogInformation("Message received from [{0}] queue and successfully converted to dataType [{1}]", entityPath, typeof(T));
                        await queueClient.CompleteAsync(message.SystemProperties.LockToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Unable to parse response from [{0}] queue to dataType [{1}]. Reason: {2}", entityPath, typeof(T), e.Message);
                    }
                }, messageHandler);
                
                // For A Console App. To make Application remain active
                //Console.ReadLine();
                //queueClient.CloseAsync();
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to register a listen for queue [{0}]. Reason: {1}", entityPath, e.Message);
            }
        }
        
        
        
    }
}