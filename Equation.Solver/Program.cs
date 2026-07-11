using Equation.Solver.DataSources.Formats.InputProblemFormat;
using Equation.Solver.DataSources.Formats.SomeDatasetFormat;
using Equation.Solver.DataSources.JsonLines;
using Equation.Solver.Solvers;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

namespace Equation.Solver;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        IAsyncEnumerable<InputProblemJsonFormat> jsonProblems = JsonLinesConverter.ReadJsonLines<SomeDatasetJsonFormat, InputProblemJsonFormat>(
            @"",
            x =>
            {
                const string thinkingEnd = "</think>";
                return new InputProblemJsonFormat
                {
                    Input = x.Input,
                    Output = x.Output.AsSpan(x.Output.IndexOf(thinkingEnd) + thinkingEnd.Length).ToString()
                };
            });

        const int prefixExampleCount = 5;
        ExampleGenerator[] examples = await CreateExamplesFromInputProblem(jsonProblems, prefixExampleCount).ToArrayAsync();

        ProblemCollection fullProblemCollection = ProblemExample.ConvertToExamples(examples, prefixExampleCount);
        examples = null!;

        (ProblemCollection solvingProblemCollection, ProblemCollection validationProblemCollection) = SplitSolvingAndValidationExamples(fullProblemCollection);
        Console.WriteLine($"Total output bits: {solvingProblemCollection.Examples.Sum(x => x.Output.Outputs.LongLength * PopCount(x.Output.MaskBitsUsed)):N0}");

        IExampleCluster exampleCluster = new RandomExampleCluster([
            new RandomExampleClustering(1.00f, 5),
            new RandomExampleClustering(0.30f, 11),
            new RandomExampleClustering(0.10f, 15),
            //new RandomExampleClustering(0.01f, 20),
        ]);
        List<ProblemExample>[] problemClusters = exampleCluster.ToClusters(solvingProblemCollection.Examples);

        var problems = problemClusters.Select(x => new EquationProblem(solvingProblemCollection.CreateSubset(x.ToArray()))).ToArray();

        var validationProblem = new EquationProblem(validationProblemCollection);

        //ISolver solver = new ParallelSolver(new RandomSolver(200));
        //ISolver solver = new ParallelSolver(new EvolveBestSolver(20000, 0.0002f));
        //ISolver solver = new ParallelSolver(new RandomEvolutionSolver(problem.ParameterCount, 1000, 100_000, 0.1f, 0.0025f, 0.0001f, 0.5f));
        //ISolver solver = new ParallelSolver(new RandomEvolutionSolverWithEquationCombining(problem.ParameterCount, 1000, problem.OutputCount, 100_000, 0.01f, 0.0025f, 0.001f, 0.001f, 0.5f));
        //ISolver solver = new RandomChunkEvolutionSolver(100, 10_000, new RandomChunkEvolver(200, 10_000, 0.1f, 0.02f, problem.ParameterCount, problem.OutputCount));
        int operatorCount = 30_000;
        ISolver solver = new ParallelMixSolver(new RandomEvolutionSolverWithEquationCombining(problems[0].ParameterCount,
                                                                                              operatorCount,
                                                                                              problems[0].OutputCount,
                                                                                              20_0,
                                                                                              0.01f,
                                                                                              3,
                                                                                              0.01f,
                                                                                              0.001f,
                                                                                              0.001f,
                                                                                              10),
                                               problems,
                                               problems[0],
                                               operatorCount,
                                               30,
                                               0.02f);
        await RunSolver(solver, null!, validationProblem, operatorCount);
    }

    private static int PopCount(Vector256<int> value)
    {
        int count = 0;
        for (int i = 0; i < Vector256<int>.Count; i++)
        {
            count += BitOperations.PopCount((uint)value.GetElement(i));
        }

        return count;
    }

    private static async IAsyncEnumerable<ExampleGenerator> CreateExamplesFromInputProblem(IAsyncEnumerable<InputProblemJsonFormat> problems,
                                                                                           int examplesPerProblem)
    {
        var random = new Random(42);
        HashSet<int> usedIndexes = [];

        await foreach (var problem in problems)
        {
            yield return ExampleGenerator.CreateRandom(random, usedIndexes, problem.Input, problem.Output, examplesPerProblem);
        }
    }

    private static async Task RunSolver(ISolver solver, EquationProblem problem, EquationProblem validationProblem, int operatorCount)
    {
        var averageIterationsPerSecond = new SampleAverage(10);
        long prevIterationCount = 0;
        using var cancellation = new CancellationTokenSource();
        using var validationEquationValues = new EquationValues(validationProblem.ParameterCount, operatorCount);
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
            var validationResult = CountCorrectExamples(validationProblem, report.BestEquation, validationEquationValues);
            Console.WriteLine($"Validation examples correct: {validationResult.Correct:N0}/{validationResult.Total:N0}");
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

    private static (int Correct, int Total) CountCorrectExamples(EquationProblem validationProblem, ProblemEquation equation, EquationValues equationValues)
    {
        int correct = 0;
        int total = 0;
        foreach (var result in validationProblem.GetExampleCorrectness(equation, equationValues))
        {
            total++;
            if (result)
            {
                correct++;
            }
        }

        return (correct, total);
    }

    private static (ProblemCollection Solving, ProblemCollection Validation) SplitSolvingAndValidationExamples(ProblemCollection problemCollection)
    {
        int validationCount = problemCollection.Examples.Length / 10;
        var splitRandom = new Random(42);
        var shuffled = problemCollection.Examples.OrderBy(_ => splitRandom.Next()).ToArray();
        var validationExamples = shuffled[..validationCount];
        var solvingExamples = shuffled[validationCount..];
        return (problemCollection.CreateSubset(solvingExamples), problemCollection.CreateSubset(validationExamples));
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

    internal static bool[] TextToBools(string text)
    {
        return TextToBools(Encoding.UTF8.GetBytes(text));
    }

    internal static bool[] TextToBools(ReadOnlySpan<byte> utf8Bytes)
    {
        const int bitsPerByte = 8;
        bool[] bools = new bool[utf8Bytes.Length * bitsPerByte];
        int boolIndex = 0;
        for (int byteIndex = 0; byteIndex < utf8Bytes.Length; byteIndex++)
        {
            for (int bitIndex = 0; bitIndex < bitsPerByte; bitIndex++)
            {
                bools[boolIndex] = ((utf8Bytes[byteIndex] >> bitIndex) & 1) == 1;
                boolIndex++;
            }
        }

        return bools;
    }
}