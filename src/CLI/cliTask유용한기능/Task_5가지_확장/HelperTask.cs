using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_5가지_확장
{
    internal static class HelperTask
    {
        /// <summary>
        /// 발생하고 잊어 버리기
        /// </summary>
        /// <param name="task">Func</param>
        /// <param name="errorHandler">Error 발생시 Handeler</param>
        /// 사용법
        /// SendEmailAsync().FireAndForget(errorHandler => Console.WriteLine(errorHandler.Message));
        public static void FireAndForget(this Task task, Action<Exception> errorHandler = null)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && errorHandler != null)
                    errorHandler(t.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        /// <summary>
        /// 재시도 테스크
        /// </summary>
        /// <typeparam name="TResult">실행결과</typeparam>
        /// <param name="taskFactory">실행 함수</param>
        /// <param name="maxRetries">시도</param>
        /// <param name="delay">딜레이</param>
        /// <returns></returns>
        /// 사용법
        /// var result = await (() => GetResultAsync()).Retry(3, TimeSpan.FromSeconds(1));
        public static async Task<TResult> Retry<TResult>(this Func<Task<TResult>> taskFactory, int maxRetries, TimeSpan delay)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await taskFactory().ConfigureAwait(false);
                }
                catch
                {
                    if (i == maxRetries - 1)
                        throw;
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }

            return default(TResult); // Should not be reached
        }
        /// <summary>
        /// Task 에서 예외 발생 시 콜백 함수 실행
        /// </summary>
        /// <param name="task"></param>
        /// <param name="onFailure"></param>
        /// <returns></returns>
        /// 사용 법 
        /// await GetResultAsync().OnFailure(ex => Console.WriteLine(ex.Message));
        public static async Task OnFailure(this Task task, Action<Exception> onFailure)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                onFailure(ex);
            }
        }
        /// <summary>
        /// 작업에 시간 제한을 설정하고 싶을 때 사용, 오래 실행되는것을 방지하는 경우 유용
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        /// 사용
        /// await GetResultAsync().WithTimeout(TimeSpan.FromSeconds(1));
        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var delayTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(task, delayTask);
            if (completedTask == delayTask)
                throw new TimeoutException();

            await task;
        }
        /// <summary>
        /// 작업이 실패 할 때 대체 값을 사용하려는 경우 사용
        /// </summary>
        /// <typeparam name="TResult">리턴 타입</typeparam>
        /// <param name="task">실행 함수</param>
        /// <param name="fallbackValue">실패시 리터 결과</param>
        /// <returns></returns>
        /// 사용
        /// var result = await GetResultAsync().Fallback("fallback");
        public static async Task<TResult> Fallback<TResult>(this Task<TResult> task, TResult fallbackValue)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                return fallbackValue;
            }
        }

    }
}
