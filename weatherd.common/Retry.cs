using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace weatherd
{
    public static class Retry
    {
        private const int DefaultRetryInterval = 5000;
        private const int DefaultRetryAttempts = 5;

        /// <summary>
        /// Provides retry logic for an action that returns a boolean success flag.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static bool Do(Func<bool> action)
            => Do(action, TimeSpan.FromMilliseconds(DefaultRetryInterval), DefaultRetryAttempts);

        /// <summary>
        /// Provides retry logic for an action that returns a boolean success flag.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <param name="retryIntervalMs">The time to wait between attempts in milliseconds</param>
        /// <param name="maxAttemptCount">The maximum number of attempts before failing</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static bool Do(Func<bool> action, int retryIntervalMs, int maxAttemptCount)
            => Do(action, TimeSpan.FromMilliseconds(retryIntervalMs), maxAttemptCount);

        /// <summary>
        /// Provides retry logic for an action that returns a boolean success flag.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <param name="retryInterval">The time to wait between attempts</param>
        /// <param name="maxAttemptCount">The maximum number of attempts before failing</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static bool Do(Func<bool> action, TimeSpan retryInterval, int maxAttemptCount)
        {
            var exceptions = new Stack<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                        Thread.Sleep(retryInterval);

                    if (action())
                        return true;
                }
                catch (Exception ex)
                {
                    exceptions.Push(ex);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            return false;
        }
        
        /// <summary>
        /// Provides asynchronous retry logic for an action that returns a boolean success flag.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static Task<bool> DoAsync(Func<Task<bool>> action)
            => DoAsync(action, TimeSpan.FromMilliseconds(DefaultRetryInterval), DefaultRetryAttempts);

        /// <summary>
        /// Provides asynchronous retry logic for an action that returns a boolean success flag.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <param name="retryIntervalMs">The time to wait between attempts in milliseconds</param>
        /// <param name="maxAttemptCount">The maximum number of attempts before failing</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static Task<bool> DoAsync(Func<Task<bool>> action, int retryIntervalMs, int maxAttemptCount)
            => DoAsync(action, TimeSpan.FromMilliseconds(retryIntervalMs), maxAttemptCount);

        /// <summary>
        /// Provides asynchronous retry logic for an action that returns a boolean success flag.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <param name="retryInterval">The time to wait between attempts</param>
        /// <param name="maxAttemptCount">The maximum number of attempts before failing</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static async Task<bool> DoAsync(Func<Task<bool>> action, TimeSpan retryInterval, int maxAttemptCount)
        {
            var exceptions = new Stack<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                        await Task.Delay(retryInterval);

                    if (await action())
                        return true;
                }
                catch (Exception ex)
                {
                    exceptions.Push(ex);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            return false;
        }
        
        
        /// <summary>
        /// Attempts to call an action perpetually, retrying a number of times upon failure.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static Task<bool> DoPerpetuallyAsync(Func<Task<bool>> action)
            => DoPerpetuallyAsync(action, TimeSpan.FromMilliseconds(DefaultRetryInterval), DefaultRetryAttempts);

        /// <summary>
        /// Attempts to call an action perpetually, retrying a number of times upon failure.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <param name="retryIntervalMs">The time to wait between attempts in milliseconds</param>
        /// <param name="maxAttemptCount">The maximum number of attempts before failing</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static Task<bool> DoPerpetuallyAsync(Func<Task<bool>> action, int retryIntervalMs, int maxAttemptCount)
            => DoPerpetuallyAsync(action, TimeSpan.FromMilliseconds(retryIntervalMs), maxAttemptCount);

        /// <summary>
        /// Attempts to call an action perpetually, retrying a number of times upon failure.
        /// </summary>
        /// <param name="action">The action to retry</param>
        /// <param name="retryInterval">The time to wait between attempts</param>
        /// <param name="maxAttemptCount">The maximum number of attempts before failing</param>
        /// <returns>True if the action was successful, otherwise false</returns>
        public static async Task<bool> DoPerpetuallyAsync(Func<Task<bool>> action, TimeSpan retryInterval, int maxAttemptCount)
        {
            var exceptions = new Stack<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                        await Task.Delay(retryInterval);

                    if (await action())
                        attempted = 0;
                }
                catch (Exception ex)
                {
                    exceptions.Push(ex);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            return false;
        }
    }
}
