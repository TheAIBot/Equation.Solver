using Equation.Solver.Solvers;

namespace Equation.Solver;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        ProblemExample[] examples = ProblemExample.ConvertToExamples(CreateBiArgOperatorExamplesAsInts(10_000, 16, (x, y) => x + y)).ToArray();

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

            Console.WriteLine();
            Console.WriteLine($"Iterations: {report.IterationCount:N0}");
            Console.WriteLine($"Iterations/s: {averageIterationsPerSecond.GetAverage():N0}");
            Console.WriteLine($"Best score: {report.BestScore:N0}");
            if (solver is IMultipleReporting multiReporting)
            {
                SolverReport[] reports = multiReporting.GetAllReports();
                string[] scores = reports.Select(x => x.BestScore.ToString("N0")).ToArray();
                int maxLengthScore = scores.Max(x => x.Length);

                Console.WriteLine($"All Reported scores: {string.Join(", ", scores.Select(x => x.PadLeft(maxLengthScore)))}");
            }

            if (report.BestScore == 0)
            {
                cancellation.Cancel();
                break;
            }
        }
    }

    private static IEnumerable<(bool[] inputs, bool[] outputs)> CreateBiArgOperatorExamplesAsInts(int exampleCount, int bitCount, Func<int, int, int> function)
    {
        for (int exampleCounter = 0; exampleCounter < exampleCount; exampleCounter++)
        {
            bool[] inputs = new bool[bitCount * 2];
            Span<bool> leftInput = inputs.AsSpan(0, bitCount);
            Span<bool> rightInput = inputs.AsSpan(bitCount, bitCount);
            bool[] outputs = new bool[bitCount];

            int leftValue = exampleCounter;
            int rightValue = exampleCounter + 1;
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