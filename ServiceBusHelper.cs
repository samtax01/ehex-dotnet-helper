using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable once CheckNamespace
#pragma warning disable 1570
namespace Ehex.Helpers
{


    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    /// </summary>
    public static class ServiceBusHelper
    {
        private static TopicClient _topicClient;
        private static QueueClient _queueClient;

        /// <summary>
        /// Serialize Message Object to JSON/Byte
        /// </summary>
        /// <param name="messageObject">Message Payload</param>
        /// <param name="messageType">Use for filter. default: The Class name</param>
        /// <param name="messageEnvironment">Specified your message environment to distinguish between production and development queue. You can use App Setting Config to do this</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static Message GetMessage<T>(T messageObject, string messageType = null, string messageEnvironment = "dev")
        {
            var messageContent = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageObject));
            var message = new Message(messageContent);
            message.UserProperties["MessageType"] = messageType?? typeof(T).Name;
            message.UserProperties["messageEnvironment"] = messageEnvironment;
            return message;
        }
        
        /// <summary>
        /// Message Handler
        /// </summary>
        /// <returns></returns>
        private static MessageHandlerOptions GetMessageHandler(string entityPath, ILogger logger)
        {
            return new MessageHandlerOptions(
                e => {
                    logger.LogError("Unable to read data from queue [{0}]. Reason: {1}", entityPath, e.Exception.Message);
                    return Task.CompletedTask;
                }
            )
            {
                
                MaxConcurrentCalls = 1, // Total Item to run at a time.
                AutoComplete = false, // Delay until after Queue TTL. (30s)
            };
        }


        public static class Topic
        {
            /// <summary>
            /// Broadcast to Topic
            /// </summary>
            /// <param name="messageObject">Message Payload</param>
            /// <param name="topicClient"></param>
            /// <param name="messageType">Use for filter. default: The Class name</param>
            /// <param name="messageEnvironment">Specified your message environment to distinguish between production and development queue. You can use App Setting Config to do this</param>
            /// <param name="url"></param>
            /// <param name="entityPath"></param>
            /// <param name="logger"></param>
            /// <param name="throwException"></param>
            public static async Task SendMessage<T>(T messageObject, string url, string entityPath, ILogger logger, TopicClient topicClient = null, string messageType = null, string messageEnvironment = "dev", bool throwException = false) 
            {
                try
                {
                    // Get a valid instance
                    _topicClient = topicClient ?? _topicClient ?? new TopicClient(url, entityPath);
                    await _topicClient.SendAsync(GetMessage(messageObject, messageType, messageEnvironment));
                }
                catch (Exception e)
                {
                    logger.LogError("Unable to send to [{0}] queue topic. Reason: {1}", entityPath, e.Message);
                    if (throwException) throw;
                }
            }

            /// <summary>
            /// Listen for a queue topic event
            /// Create to Application Startup part where a listener can be registered.
            /// <br/> If you get error "Cannot access a disposed context instance.". Simply add ServiceLifetime.Singleton
            ///  <br/>  e.g services.AddDbContext<DatabaseContext/>(options => options.UseSqlServer( Configuration.GetConnectionString("ms_db") ), ServiceLifetime.Singleton);
            /// </summary>
            public static void ReceiveMessage<T>(Func<T, Task> callBackMethod, string url, string entityPath, string subscriptionName, ILogger logger, SubscriptionClient subscriptionClient = null, bool throwException = false)
            {
                try
                {
                    // Get a valid instance
                    var subscription = subscriptionClient ?? new SubscriptionClient(url, entityPath, subscriptionName);
                    
                    // Callback method
                    subscription.RegisterMessageHandler(async (message, cancellationToken) =>
                    {
                        try
                        {
                            var json = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
                            await callBackMethod(json);
                            logger.LogInformation("Message received from [{0}] queue and successfully converted to dataType [{1}]", entityPath, typeof(T).Name);
                            await subscription.CompleteAsync(message.SystemProperties.LockToken);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Unable to parse response from [{0}] queue topic to dataType [{1}]. Reason: {2}", entityPath, typeof(T).Name, e.Message);
                            if (throwException) throw;
                        }
                    }, GetMessageHandler(entityPath, logger));
                }
                catch (Exception e)
                {
                    logger.LogError("Unable to register a listener for subscription [{0}] in [{1}]. Reason: {2}", subscriptionName, entityPath, e.Message);
                    if (throwException) throw;
                }
            }
        }
        
        
        public static class Queue
        {
            /// <summary>
            /// Broadcast to Queue
            /// </summary>
            /// <param name="messageObject"></param>
            /// <param name="url"></param>
            /// <param name="entityPath"></param>
            /// <param name="logger"></param>
            /// <param name="queueClient"></param>
            /// <param name="messageType">Use for filtering object type</param>
            /// <param name="messageEnvironment">Use for filtering environment type. Identify if the queue message belongs to dev or prod environment. You can use App/Dev Setting Config to change this</param>
            /// <param name="throwException"></param>
            /// <typeparam name="T"></typeparam>
            public static async Task SendMessage<T>(T messageObject, string url, string entityPath, ILogger logger, QueueClient queueClient = null, string messageType = null, string messageEnvironment = "dev", bool throwException = false) 
            {
                try
                {
                    // Get a valid instance
                    _queueClient = queueClient ?? _queueClient ?? new QueueClient(url, entityPath);
                    await _queueClient.SendAsync(GetMessage(messageObject, messageType, messageEnvironment));
                }
                catch (Exception e)
                {
                    logger.LogError("Unable to send to [{0}] queue. Reason: {1}", entityPath, e.Message);
                    if (throwException) throw;
                }
            }

         

            
            
            
            
            /// <summary>
            /// Listen for a queue event
            /// Create to Application Startup part where a listener can be registered.
            /// <br/> If you get error "Cannot access a disposed context instance.". Simply add ServiceLifetime.Singleton
            ///  <br/>  e.g services.AddDbContext<DatabaseContext/>(options => options.UseSqlServer( Configuration.GetConnectionString("ms_db") ), ServiceLifetime.Singleton);
            /// </summary>
            public static void ReceiveMessage<T>(Func<T, Task> callBackMethod, string url, string entityPath, ILogger logger, QueueClient queueClient = null, bool throwException = false)
            {
                try
                {
                    // Get a valid instance
                    var queue = queueClient ?? new QueueClient(url, entityPath);
                    
                    // Callback method
                    queue.RegisterMessageHandler(async (message, cancellationToken) =>
                    {
                        try
                        {
                            var json = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
                            await callBackMethod(json);
                            logger.LogInformation("Message received from [{0}] queue and successfully converted to dataType [{1}]", entityPath, typeof(T).Name);
                            await queue.CompleteAsync(message.SystemProperties.LockToken);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Unable to parse response from [{0}] queue to dataType [{1}]. Reason: {2}", entityPath, typeof(T).Name, e.Message);
                            if (throwException) throw;
                        }
                    }, GetMessageHandler(entityPath, logger));
                }
                catch (Exception e)
                {
                    logger.LogError("Unable to register a listener for queue [{0}]. Reason: {1}",  entityPath, e.Message);
                    if (throwException) throw;
                }
            }
            
        }


    }
}