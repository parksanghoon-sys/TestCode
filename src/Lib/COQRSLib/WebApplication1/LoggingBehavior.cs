using LibCQRS;

namespace WebApplication1
{
    // Pipeline behavior
    public class LoggingBehavior1<TInput, TOutput> : IPipelineBehavior<TInput, TOutput>
    {
        public async Task<TOutput> HandleAsync(TInput input, Func<Task<TOutput>> next, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Starting1: {typeof(TInput).Name}");
            var result = await next();
            Console.WriteLine($"Finished1: {typeof(TOutput).Name}");
            return result;
        }
    }
    public class LoggingBehavior2<TInput, TOutput> : IPipelineBehavior<TInput, TOutput>
    {
        public async Task<TOutput> HandleAsync(TInput input, Func<Task<TOutput>> next, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Starting2: {typeof(TInput).Name}");
            var result = await next();
            Console.WriteLine($"Finished2: {typeof(TOutput).Name}");
            return result;
        }
    }
    public class LoggingBehavior3<TInput, TOutput> : IPipelineBehavior<TInput, TOutput>
    {
        public async Task<TOutput> HandleAsync(TInput input, Func<Task<TOutput>> next, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Starting3: {typeof(TInput).Name}");
            var result = await next();
            Console.WriteLine($"Finished3: {typeof(TOutput).Name}");
            return result;
        }
    }
}
