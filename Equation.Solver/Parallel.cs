using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

namespace Equation.Solver;

internal static class ParallelExtensions
{
    extension(Parallel)
    {
        public static IAsyncEnumerable<TTo> Transform<TFrom, TTo>(IEnumerable<TFrom> from, Func<TFrom, TTo> transform)
        {
            return Transform(from.ToAsyncEnumerable(), x => ValueTask.FromResult(transform(x)));
        }

        public static IAsyncEnumerable<TTo> Transform<TFrom, TTo>(IAsyncEnumerable<TFrom> from, Func<TFrom, TTo> transform)
        {
            return Transform(from, x => ValueTask.FromResult(transform(x)));
        }

        public static IAsyncEnumerable<TTo> Transform<TFrom, TTo>(IEnumerable<TFrom> from, Func<TFrom, ValueTask<TTo>> transform)
        {
            return Transform(from.ToAsyncEnumerable(), transform);
        }

        public static async IAsyncEnumerable<TTo> Transform<TFrom, TTo>(IAsyncEnumerable<TFrom> from, Func<TFrom, ValueTask<TTo>> transform)
        {
            var transformer = new TransformBlock<TFrom, ValueOrException<TTo>>(
                async x =>
                {
                    try
                    {
                        return new ValueOrException<TTo>(await transform(x), null);
                    }
                    catch (Exception e)
                    {
                        return new ValueOrException<TTo>(default, e);
                    }
                },
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount - 1,
                    SingleProducerConstrained = true,
                    EnsureOrdered = true
                });

            Task addValues = Task.Run(async () =>
            {
                try
                {
                    await foreach (var value in from)
                    {
                        if (!await transformer.SendAsync(value))
                        {
                            throw new InvalidOperationException("Failed to enqueue element.");
                        }
                    }
                }
                finally
                {
                    transformer.Complete();
                }
            });

            await foreach (var result in transformer.ReceiveAllAsync())
            {
                if (result.HasException)
                {
                    throw result.Exception;
                }

                yield return result.Value;
            }

            await addValues;
        }
    }



    private readonly record struct ValueOrException<T>(T? Value, Exception? Exception)
    {
        [MemberNotNullWhen(true, nameof(Exception))]
        [MemberNotNullWhen(false, nameof(Value))]
        public bool HasException => Exception != null;
    }
}
