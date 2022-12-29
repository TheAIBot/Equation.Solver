using Equation.Solver.Solvers;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        ProblemExample[] examples = CreateBiArgOperatorExamples(1000, 16, (x, y) => x + y).ToArray();

        var problem = new EquationProblem(examples);
        //ISolver solver = new ParallelSolver(new RandomSolver(20));
        //ISolver solver = new ParallelSolver(new RandomEvolutionSolver(100, 100_000, 0.1f, 0.025f));
        ISolver solver = new RandomChunkEvolutionSolver(100, new RandomChunkEvolver(200, 10_000, 0.1f, 0.02f, problem.ParameterCount, problem.OutputCount));


        await RunSolver(solver, problem);
    }

    private static async Task RunSolver(ISolver solver, EquationProblem problem)
    {
        var averageIterationsPerSecond = new SampleAverage(10);
        long prevIterationCount = 0;
        var cancellation = new CancellationTokenSource();
        Task solverTask = Task.Run(() => solver.SolveAsync(problem, cancellation.Token));

        PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(cancellation.Token))
        {
            SolverReport? report = solver.GetReport();
            if (report == null)
            {
                Console.WriteLine("No reports available");
                continue;
            }

            long iterationsSinceLastReport = report.IterationCount - prevIterationCount;
            prevIterationCount = report.IterationCount;
            averageIterationsPerSecond.AddSample(iterationsSinceLastReport);

            SolverReport[] reports = solver is IMultipleReporting multiReporting ? multiReporting.GetAllReports() : new SolverReport[] { report };

            Console.WriteLine($"{report.IterationCount:N0}, {averageIterationsPerSecond.GetAverage():N0}, {string.Join(", ", reports.Select(x => x.BestScore.ToString().PadLeft(5)))}");
            if (report.BestScore == 0)
            {
                cancellation.Cancel();
                break;
            }
        }
    }

    private static IEnumerable<ProblemExample> CreateBiArgOperatorExamples(int exampleCount, int bitCount, Func<int, int, int> function)
    {
        var values = CreateBiArgOperatorExamplesAsVector256(exampleCount, bitCount, function);
        foreach (var inputOutputs in values)
        {
            yield return new ProblemExample(new ProblemInput(inputOutputs.inputs), new ProblemOutput(inputOutputs.outputs));
        }
    }

    private static IEnumerable<(Vector256<int>[] inputs, Vector256<int>[] outputs)> CreateBiArgOperatorExamplesAsVector256(int exampleCount, int bitCount, Func<int, int, int> function)
    {
        var values = CreateBiArgOperatorExamplesAsInts(exampleCount, bitCount, function);
        const int vector256BitCount = 256;
        const int intBitCount = 32;
        const int intsPerVector256 = vector256BitCount / intBitCount;
        foreach ((int[] inputs, int[] outputs)[] valueChunk in values.Chunk(intsPerVector256))
        {
            int[,] inputs32x8 = new int[valueChunk[0].inputs.Length, intsPerVector256];
            for (int i = 0; i < valueChunk[0].inputs.Length; i++)
            {
                for (int x = 0; x < valueChunk.Length; x++)
                {
                    inputs32x8[i, x] = valueChunk[x].inputs[i];
                }
            }

            Vector256<int>[] inputs256 = new Vector256<int>[valueChunk[0].inputs.Length];
            for (int i = 0; i < valueChunk[0].inputs.Length; i++)
            {
                inputs256[i] = Vector256.Create(inputs32x8[i, 0],
                                                inputs32x8[i, 1],
                                                inputs32x8[i, 2],
                                                inputs32x8[i, 3],
                                                inputs32x8[i, 4],
                                                inputs32x8[i, 5],
                                                inputs32x8[i, 6],
                                                inputs32x8[i, 7]);
            }

            int[,] outputs32x8 = new int[valueChunk[0].outputs.Length, intsPerVector256];
            for (int i = 0; i < valueChunk[0].outputs.Length; i++)
            {
                for (int x = 0; x < valueChunk.Length; x++)
                {
                    outputs32x8[i, x] = valueChunk[x].outputs[i];
                }
            }

            Vector256<int>[] outputs256 = new Vector256<int>[valueChunk[0].outputs.Length];
            for (int i = 0; i < valueChunk[0].outputs.Length; i++)
            {
                outputs256[i] = Vector256.Create(outputs32x8[i, 0],
                                                 outputs32x8[i, 1],
                                                 outputs32x8[i, 2],
                                                 outputs32x8[i, 3],
                                                 outputs32x8[i, 4],
                                                 outputs32x8[i, 5],
                                                 outputs32x8[i, 6],
                                                 outputs32x8[i, 7]);
            }

            yield return (inputs256, outputs256);
        }
    }

    private static IEnumerable<(int[] inputs, int[] outputs)> CreateBiArgOperatorExamplesAsInts(int exampleCount, int bitCount, Func<int, int, int> function)
    {
        const int intBitCount = 32;
        int[] inputs = new int[bitCount * 2];
        Span<int> leftInput = inputs.AsSpan(0, bitCount);
        Span<int> rightInput = inputs.AsSpan(bitCount, bitCount);
        int[] outputs = new int[bitCount];

        int exampleCounter = 0;
        while (exampleCounter < exampleCount)
        {
            int leftValue = exampleCounter;
            int rightValue = exampleCounter + 1;
            int outputValue = function(leftValue, rightValue);

            for (int i = 0; i < bitCount; i++)
            {
                int leftBit = (leftValue >> i) & 1;
                int rightBit = (rightValue >> i) & 1;
                int outputBit = (outputValue >> i) & 1;
                leftInput[i] |= leftBit;
                rightInput[i] |= rightBit;
                outputs[i] |= outputBit;
            }

            exampleCounter++;
            if (exampleCounter % intBitCount == 0)
            {
                yield return (inputs, outputs);
                inputs = new int[bitCount * 2];
                leftInput = inputs.AsSpan(0, bitCount);
                rightInput = inputs.AsSpan(bitCount, bitCount);
                outputs = new int[bitCount];
            }
            else if (exampleCounter < exampleCount)
            {
                for (int i = 0; i < inputs.Length; i++)
                {
                    inputs[i] <<= 1;
                }
                for (int i = 0; i < outputs.Length; i++)
                {
                    outputs[i] <<= 1;
                }
            }
        }

        if (exampleCounter % intBitCount != 0)
        {
            yield return (inputs, outputs);
        }
    }
}