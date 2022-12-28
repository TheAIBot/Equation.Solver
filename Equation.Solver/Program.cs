using Equation.Solver.Solvers;

namespace Equation.Solver;

internal class Program
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
        CancellationTokenSource cancellation = new CancellationTokenSource();
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

            SolverReport[] reports = solver is IMultipleReporting multiReporting ? multiReporting.GetAllReports() : new SolverReport[] { report };

            Console.WriteLine($"{report.IterationCount:N0}, {string.Join(", ", reports.Select(x => x.BestScore.ToString().PadLeft(5)))}");
            if (report.BestScore == 0)
            {
                cancellation.Cancel();
                break;
            }
        }
    }

    private static IEnumerable<ProblemExample> CreateBiArgOperatorExamples(int exampleCount, int bitCount, Func<int, int, int> function)
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
                yield return new ProblemExample(new ProblemInput(inputs), new ProblemOutput(outputs), intBitCount);
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
            yield return new ProblemExample(new ProblemInput(inputs), new ProblemOutput(outputs), exampleCounter % intBitCount);
        }
    }
}