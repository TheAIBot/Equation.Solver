using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class RandomSolver : ISolver
{
    private readonly int _operatorCount;
    private long _iterationCount;
    private int _bestScore;
    [AllowNull]
    private ProblemEquation _bestEquation;
    private bool _isRunning = false;

    public RandomSolver(int operatorCount)
    {
        _operatorCount = operatorCount;
    }

    public SolverReport? GetReport()
    {
        if (!_isRunning)
        {
            return null;
        }
        return new SolverReport(_iterationCount, _bestScore, _bestEquation);
    }

    public Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        _isRunning = true;
        try
        {
            var random = new Random();
            var equation = new ProblemEquation(problem.ParameterCount, _operatorCount, problem.OutputCount);

            _iterationCount = 0;
            _bestScore = int.MaxValue;
            while (_bestScore != 0 && !cancellationToken.IsCancellationRequested)
            {
                _iterationCount++;
                Randomize(random, equation);

                int score = problem.EvaluateEquation(equation);
                if (score < _bestScore)
                {
                    _bestScore = score;
                    _bestEquation = equation.Copy();
                }
            }

            return Task.CompletedTask;
        }
        finally
        {
            _isRunning = false;
        }
    }

    public ISolver Copy()
    {
        return new RandomSolver(_operatorCount);
    }

    internal static void Randomize(Random random, ProblemEquation equation)
    {
        Span<NandOperator> operators = equation.NandOperators;
        int staticResultSize = equation.StaticResultSize;
        for (int i = 0; i < operators.Length; i++)
        {
            int leftValueIndex = random.Next(0, staticResultSize + i);
            int rightValueIndex = random.Next(0, staticResultSize + i);
            operators[i] = new NandOperator(leftValueIndex, rightValueIndex);
        }
    }
}
