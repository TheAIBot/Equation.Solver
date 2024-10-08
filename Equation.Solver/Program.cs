﻿using Equation.Solver.Solvers;

namespace Equation.Solver;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        ProblemExample[] examples = ProblemExample.ConvertToExamples(CreateBiArgOperatorExamplesAsInts(1_000, 10, (x, y) => x + y)).ToArray();

        var problem = new EquationProblem(examples);
        //ISolver solver = new ParallelSolver(new RandomSolver(200));
        //ISolver solver = new ParallelSolver(new EvolveBestSolver(20000, 0.0002f));
        //ISolver solver = new ParallelSolver(new RandomEvolutionSolver(problem.ParameterCount, 1000, 100_000, 0.1f, 0.0025f, 0.0001f, 0.5f));
        ISolver solver = new ParallelSolver(new RandomEvolutionSolverWithEquationCombining(problem.ParameterCount, 1000, problem.OutputCount, 100_000, 0.01f, 0.0025f, 0.001f, 0.001f, 0.5f));
        //ISolver solver = new RandomChunkEvolutionSolver(100, 10_000, new RandomChunkEvolver(200, 10_000, 0.1f, 0.02f, problem.ParameterCount, problem.OutputCount));


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
}