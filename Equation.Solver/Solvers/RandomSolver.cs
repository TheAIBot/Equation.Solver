using Equation.Solver.Score;
using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class RandomSolver : ISolver
{
    private readonly int _operatorCount;
    private readonly FullScorer _fullScorer;
    private long _iterationCount;
    private EquationScore _bestScore;
    [AllowNull]
    private ProblemEquation _bestEquation;
    private bool _isRunning = false;

    public RandomSolver(int operatorCount)
    {
        _operatorCount = operatorCount;
        _fullScorer = new FullScorer();
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
            var equation = new ProblemEquation(_operatorCount, problem.OutputCount);
            var equationValues = new EquationValues(problem.ParameterCount, _operatorCount);

            _iterationCount = 0;
            _bestScore = EquationScore.MaxScore;
            while (_bestScore.WrongBits != 0 && !cancellationToken.IsCancellationRequested)
            {
                _iterationCount++;
                Randomize(random, equation, equationValues);

                SlimEquationScore score = problem.EvaluateEquation(equation, equationValues);
                if (score < _bestScore)
                {
                    _bestScore = _fullScorer.ToFullScore(score, equationValues, equation);
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

    internal static void Randomize(Random random, ProblemEquation equation, EquationValues equationValues)
    {
        Span<NandOperator> operators = equation.NandOperators;
        int inputParameterCount = equationValues.InputParameterCount;
        for (int i = 0; i < operators.Length; i++)
        {
            int leftValueIndex = random.Next(0, inputParameterCount + i);
            int rightValueIndex = random.Next(0, inputParameterCount + i);
            operators[i] = new NandOperator(leftValueIndex, rightValueIndex);
        }

        equation.RecalculateOperatorsUsed(inputParameterCount);
    }
}
