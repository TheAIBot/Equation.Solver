using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class EvolveBestSolver : ISolver
{
    private readonly int _operatorCount;
    private readonly float _candidateRandomizationRate;
    private long _iterationCount;
    private EquationScore _bestScore;
    [AllowNull]
    private ProblemEquation _bestEquation;
    private bool _isRunning = false;

    public EvolveBestSolver(int operatorCount, float candidateRandomizationRate)
    {
        _operatorCount = operatorCount;
        _candidateRandomizationRate = candidateRandomizationRate;
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
            do
            {
                RandomSolver.Randomize(random, equation, equationValues);
                _bestScore = EquationScore.MaxScore;
                _bestEquation = equation.Copy();
                int iterationsSinceImprovement = 0;
                while (_bestScore.WrongBits != 0 && !cancellationToken.IsCancellationRequested)
                {
                    _iterationCount++;
                    iterationsSinceImprovement++;
                    //equation.CopyFrom(_bestEquation);
                    //int operatorCountToRandomize = 1;// Math.Clamp(iterationsSinceImprovement / 10_000_000, 1, 5);
                    //RandomChunkEvolver.RandomizeSmallPartOfEquation(random, equation, equationValues, operatorCountToRandomize);

                    int operatorIndex = random.Next(0, equation.NandOperators.Length);
                    NandOperator copyBeforeChange = equation.NandOperators[operatorIndex];
                    int leftValueIndex = random.Next(0, equationValues.StaticResultSize + operatorIndex);
                    int rightValueIndex = random.Next(0, equationValues.StaticResultSize + operatorIndex);
                    equation.NandOperators[operatorIndex] = new NandOperator(leftValueIndex, rightValueIndex);

                    SlimEquationScore score = problem.EvaluateEquation(equation, equationValues);
                    if (score < _bestScore)
                    {
                        iterationsSinceImprovement = 0;
                        _bestScore = score.ToFullScore(equationValues, equation);
                        _bestEquation = equation.Copy();
                    }
                    else
                    {
                        equation.NandOperators[operatorIndex] = copyBeforeChange;
                    }

                    if (iterationsSinceImprovement == 100_000_000)
                    {
                        break;
                    }
                }
            } while (_bestScore.WrongBits != 0 && !cancellationToken.IsCancellationRequested);

            return Task.CompletedTask;
        }
        finally
        {
            _isRunning = false;
        }
    }

    public ISolver Copy()
    {
        return new EvolveBestSolver(_operatorCount, _candidateRandomizationRate);
    }
}
