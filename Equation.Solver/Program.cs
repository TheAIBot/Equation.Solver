using Equation.Solver.Solvers;
using System.Text;

namespace Equation.Solver;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        //IEnumerable<(bool[] inputs, bool[] outputs)> examples = CreateBiArgOperatorExamplesAsInts(1_000, 10, (x, y) => x + y);
        //ProblemExample[] examples = ProblemExample.ConvertToExamples(examples).ToArray();

        IEnumerable<(bool[] inputs, bool[] outputs)> examples = NormalizeExampleSizes(200, 100, CreateTextMathExamples(10_000, -10_000, 10_000));

        long totalOutputBits = examples.Sum(x => x.outputs.LongLength);
        Console.WriteLine($"Total output bits: {totalOutputBits:N0}");

        IExampleCluster exampleCluster = new RandomExampleCluster([
            new RandomExampleClustering(1.0f, 10),
            new RandomExampleClustering(0.3f, 10),
        ]);
        var exampleClusters = exampleCluster.ToClusters(examples);
        IEnumerable<ProblemExample[]> problemClusters = exampleClusters.Select(x => ProblemExample.ConvertToExamples(x).ToArray());

        var problems = problemClusters.Select(x => new EquationProblem(x)).ToArray();
        //ISolver solver = new ParallelSolver(new RandomSolver(200));
        //ISolver solver = new ParallelSolver(new EvolveBestSolver(20000, 0.0002f));
        //ISolver solver = new ParallelSolver(new RandomEvolutionSolver(problem.ParameterCount, 1000, 100_000, 0.1f, 0.0025f, 0.0001f, 0.5f));
        //ISolver solver = new ParallelSolver(new RandomEvolutionSolverWithEquationCombining(problem.ParameterCount, 1000, problem.OutputCount, 100_000, 0.01f, 0.0025f, 0.001f, 0.001f, 0.5f));
        //ISolver solver = new RandomChunkEvolutionSolver(100, 10_000, new RandomChunkEvolver(200, 10_000, 0.1f, 0.02f, problem.ParameterCount, problem.OutputCount));
        int operatorCount = 10_000;
        ISolver solver = new ParallelMixSolver(new RandomEvolutionSolverWithEquationCombining(problems[0].ParameterCount, operatorCount, problems[0].OutputCount, 10_000, 0.01f, 0.0025f, 0.01f, 0.001f, 0.1f),
                                               problems,
                                               problems[0],
                                               operatorCount,
                                               100,
                                               0.02f);
        //await RunSolver(solver, problem);
        await RunSolver(solver, null!);
    }

    private static async Task RunSolver(ISolver solver, EquationProblem problem)
    {
        var averageIterationsPerSecond = new SampleAverage(10);
        long prevIterationCount = 0;
        using var cancellation = new CancellationTokenSource();
        Task solverTask = Task.Run(() => solver.SolveAsync(problem, cancellation.Token));

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
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

            Console.WriteLine();
            Console.WriteLine($"Iterations: {report.IterationCount:N0}");
            Console.WriteLine($"Iterations/s: {averageIterationsPerSecond.GetAverage():N0}");
            Console.WriteLine($"Best score: {report.BestScore:N0}");
            Console.WriteLine($"Best score length: {report.BestScore.MaxSequentialNandGates:N0}");
            if (solver is IMultipleReporting multiReporting)
            {
                SolverReport[] reports = multiReporting.GetAllReports();
                string[] scores = reports.Select(x => x.BestScore.WrongBits.ToString("N0")).ToArray();
                int maxLengthScore = scores.Max(x => x.Length);

                Console.WriteLine($"All Reported scores: {string.Join(", ", scores.Select(x => x.PadLeft(maxLengthScore)))}");
            }

            if (report.BestScore.WrongBits == 0)
            {
                cancellation.Cancel();
                break;
            }
        }
    }

    private static IEnumerable<(bool[] inputs, bool[] outputs)> CreateBiArgOperatorExamplesAsInts(int exampleCount, int bitCount, Func<int, int, int> function)
    {
        Random random = new Random(1);
        for (int exampleCounter = 0; exampleCounter < exampleCount; exampleCounter++)
        {
            bool[] inputs = new bool[bitCount * 2];
            Span<bool> leftInput = inputs.AsSpan(0, bitCount);
            Span<bool> rightInput = inputs.AsSpan(bitCount, bitCount);
            bool[] outputs = new bool[bitCount];

            int leftValue = random.Next(0, (1 << (bitCount - 1)) + 1);
            int rightValue = random.Next(0, (1 << (bitCount - 1)) + 1);
            int outputValue = function(leftValue, rightValue);

            for (int bitIndex = 0; bitIndex < bitCount; bitIndex++)
            {
                int leftBit = (leftValue >> bitIndex) & 1;
                int rightBit = (rightValue >> bitIndex) & 1;
                int outputBit = (outputValue >> bitIndex) & 1;
                leftInput[bitIndex] = leftBit == 1;
                rightInput[bitIndex] = rightBit == 1;
                outputs[bitIndex] = outputBit == 1;
            }

            yield return (inputs, outputs);
        }
    }

    private static IEnumerable<(bool[] inputs, bool[] outputs)> NormalizeExampleSizes(int inputSize, int outputSize, IEnumerable<(bool[] inputs, bool[] outputs)> examples)
    {
        foreach (var example in examples)
        {
            var input = example.inputs;
            Array.Resize(ref input, inputSize);
            var output = example.outputs;
            Array.Resize(ref output, outputSize);
            yield return (input, output);
        }
    }

    private static IEnumerable<(bool[] inputs, bool[] outputs)> CreateTextMathExamples(int exampleCountPerBiArgSymbol,
                                                                                       int minIncusiveNumber,
                                                                                       int maxExclusiveNumber)
    {
        var random = new Random(94);
        var add = CreateTextMathExamplesForBiArgSymbol(random, exampleCountPerBiArgSymbol, minIncusiveNumber, maxExclusiveNumber, (a, b) => a + b, "+");
        var subtract = CreateTextMathExamplesForBiArgSymbol(random, exampleCountPerBiArgSymbol, minIncusiveNumber, maxExclusiveNumber, (a, b) => a - b, "-");

        return add.Concat(subtract);
    }

    private static IEnumerable<(bool[] inputs, bool[] outputs)> CreateTextMathExamplesForBiArgSymbol(Random random,
                                                                                                     int exampleCount,
                                                                                                     int minIncusiveNumber,
                                                                                                     int maxExclusiveNumber,
                                                                                                     Func<int, int, int> operation,
                                                                                                     string biArgOperationSymbol)
    {
        for (int exampleCounter = 0; exampleCounter < exampleCount; exampleCounter++)
        {
            int a = random.Next(minIncusiveNumber, maxExclusiveNumber);
            int b = random.Next(minIncusiveNumber, maxExclusiveNumber);
            int result = operation(a, b);
            string input = $"{a} {biArgOperationSymbol} {b} =";
            string output = $"{result}";

            yield return (TextToBools(input), TextToBools(output));
        }
    }

    private static bool[] TextToBools(string text)
    {
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(text);
        List<bool> bools = [];
        for (int byteIndex = 0; byteIndex < utf8Bytes.Length; byteIndex++)
        {
            const int bitsPerByte = 8;
            for (int bitIndex = 0; bitIndex < bitsPerByte; bitIndex++)
            {
                bools.Add(((utf8Bytes[byteIndex] >> bitIndex) & 1) == 1);
            }
        }

        return bools.ToArray();
    }
}