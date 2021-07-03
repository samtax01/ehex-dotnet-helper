using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
#pragma warning disable 1570
namespace Ehex.Helpers
{
   
    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    /// 
    /// Background Queue Service
    /// 
    /// Add BackgroundTaskQueue to service for DI
    ///     services.AddHostedService<BackgroundTaskQueue>();
    /// 
    /// Sample Usage for Enqueuing a task
    ///     BackgroundTaskQueue.EnqueueAsync(async token =>
    ///     {
    ///          for (var i = 0; i < 5; i++)
    ///          {
    ///              Console.WriteLine("Background task running " + i);
    ///              await Task.Delay(TimeSpan.FromSeconds(2));
    ///          }
    ///          await Task.CompletedTask;
    ///     });
    ///
    /// 
    /// Sample Usage for Scheduling  a task
    ///     BackgroundTaskQueue.Schedule((cancellableToken, index) =>
    ///     {
    ///         if(index > 10)
    ///             cancellableToken.cancel();
    ///          
    ///          Console.WriteLine("Background task running " + index);
    ///         
    ///     }, startTime, RepeatTime);
    /// </summary>


    public class BackgroundTaskQueue : BackgroundService
    {
        private static readonly IDictionary<string, KeyValuePair<CancellationTokenSource, int>> ScheduleConfigs = new Dictionary<string, KeyValuePair<CancellationTokenSource, int>>();
        private static Channel<Action<CancellationToken>> _queue;
        private readonly ILogger<BackgroundTaskQueue> _logger;
        private readonly bool _asParallel;
        
        public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger, int capacity = 100, bool asParallel = true)
        {
            // Capacity should be set based on the expected application load and
            // number of concurrent threads accessing the queue.            
            // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
            // which completes only when space became available. This leads to backpressure,
            // in case too many publishers/calls start accumulating.
            var options = new BoundedChannelOptions(capacity) {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<Action<CancellationToken>>(options);
            //_queue = Channel.CreateUnbounded<Action<CancellationToken>>();
            _logger = logger;
            _asParallel = asParallel;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var currentCallback = await DequeueAsync(stoppingToken);
                try
                {
                    if (_asParallel)
                    {
                        var task = Task.Run( () =>  currentCallback(stoppingToken), stoppingToken);
                    }
                    else currentCallback(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"Error occurred executing {0}", nameof(currentCallback));
                }
            }
        }
        

        
        /// <summary>
        /// Add a function to execute in the background
        /// </summary>
        /// <param name="callback">Callback Function</param>
        /// <exception cref="ArgumentNullException">Return null argument if null</exception>
        public static async void Enqueue(Action<CancellationToken> callback)
        {
            try
            {
                if (callback == null)
                    throw new ArgumentNullException(nameof(callback));
                await _queue.Writer.WriteAsync(callback);
            }
            catch (Exception e)
            {
                Console.WriteLine($"BackgroundTaskQueue Failed. Unable to run task in the background. Reason: {e.Message}. Ensure services.AddHostedService<BackgroundTaskQueue>(); is added to the startup");
            }
        }
        
        /// <summary>
        /// Remove a function from the queue
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async ValueTask<Action<CancellationToken>> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }

        
        


        /// <summary>
        /// Add a function to execute in the background
        /// </summary>
        /// <param name="callback">
        ///  Callback Function. Consist of
        ///     - CancellationTokenSource token to terminate schedule
        ///         e.g if (value > 5)
        ///                 token.Cancel(); or use BackgroundTaskQueue.UnSchedule("name");
        /// 
        ///     and a counter variable to show how many time the schedule as execute
        /// </param>
        /// <param name="startAt">When to start schedule execution</param>
        /// <param name="repeatEvery">
        ///     Optional. for recurring schedule.
        ///     e.g
        ///         TimeSpan.FromTicks(TimeSpan.TicksPerSecond) - every second
        ///         TimeSpan.FromTicks(TimeSpan.TicksPerDay) or TimeSpan.FromDays(1) - every day
        /// </param>
        /// <param name="uniqueName">For tracking and to un-schedule a task</param>
        public static void Schedule(Action<CancellationTokenSource, int> callback, TimeSpan  startAt = default, TimeSpan repeatEvery = default, string uniqueName = null)
        {
            try
            {
                // Setup for cancellation and value increment
                uniqueName ??= Guid.NewGuid().ToString();
                var cancelToken = new CancellationTokenSource();
                var currentValue = 0;
                if (repeatEvery != default)
                {
                    if (ScheduleConfigs.ContainsKey(uniqueName))
                    {
                        (cancelToken, currentValue) = ScheduleConfigs[uniqueName];
                        currentValue++;
                        ScheduleConfigs[uniqueName] = new KeyValuePair<CancellationTokenSource, int>(cancelToken, currentValue);
                    }
                    else
                    {
                        var cv = new KeyValuePair<CancellationTokenSource, int>(cancelToken, currentValue);
                        ScheduleConfigs.Add(uniqueName, cv);
                    }
                }

                // Run in a separate thread
                Task.Run(async () =>
                {
                    // Were we already canceled?
                    cancelToken.Token.ThrowIfCancellationRequested();
                    await Task.Delay(startAt, cancelToken.Token);
                    startAt = default;

                    // Execute The Task
                    Enqueue( _=> callback(cancelToken, currentValue));

                    // Reset for next Execution
                    if (repeatEvery != default)
                        Schedule(callback, repeatEvery, repeatEvery, uniqueName);
                    
                }, cancelToken.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"BackgroundTaskQueue Failed. Unable to schedule task. Reason: {e.Message}");
            }
        }

        /// <summary>
        /// Use to UnSchedule a task.
        ///     BackgroundTaskQueue.UnSchedule("appName");
        /// </summary>
        /// <param name="uniqueName"></param>
        public static void UnSchedule(string uniqueName)
        {
            if (!ScheduleConfigs.ContainsKey(uniqueName)) return;
            ScheduleConfigs[uniqueName].Key.Cancel();
            ScheduleConfigs[uniqueName].Key.Dispose();
            ScheduleConfigs.Remove(uniqueName);
        }

    
        
        
    }
}