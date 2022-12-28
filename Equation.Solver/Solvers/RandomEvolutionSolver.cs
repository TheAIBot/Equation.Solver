using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class RandomEvolutionSolver : ISolver
{
    private readonly int _operatorCount;
    private readonly int _candidateCount;
    private readonly float _candidateCompetitionRate;
    private readonly float _candidateRandomizationRate;
    private long _iterationCount;
    private int? _bestScore;
    [AllowNull]
    private ProblemEquation _bestEquation;
    private bool _isRunning = false;

    public RandomEvolutionSolver(int operatorCount, int candidateCount, float candidateCompetitionRate, float candidateRandomizationRate)
    {
        _operatorCount = operatorCount;
        _candidateCount = candidateCount;
        _candidateCompetitionRate = candidateCompetitionRate;
        _candidateRandomizationRate = candidateRandomizationRate;
    }

    public SolverReport? GetReport()
    {
        if (!_isRunning)
        {
            return null;
        }
        if (_bestScore == null)
        {
            return null;
        }
        return new SolverReport(_iterationCount, _bestScore.Value, _bestEquation);
    }

    public Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        _isRunning = true;
        try
        {
            var random = new Random();
            var equations = new ProblemEquation[_candidateCount];
            for (int i = 0; i < equations.Length; i++)
            {
                equations[i] = new ProblemEquation(problem.ParameterCount, _operatorCount, problem.OutputCount);
                RandomSolver.Randomize(random, equations[i]);
            }

            _iterationCount = 0;
            _bestScore = int.MaxValue;
            while (_bestScore != 0 && !cancellationToken.IsCancellationRequested)
            {
                _iterationCount += _candidateCount;
                int competitionCount = (int)(_candidateCount * _candidateCompetitionRate);
                for (int i = 0; i < competitionCount; i++)
                {
                    int firstCompetitorIndex = random.Next(0, equations.Length);
                    int secondCompetitorIndex = random.Next(0, equations.Length);
                    ProblemEquation firstEquation = equations[firstCompetitorIndex];
                    ProblemEquation secondEquation = equations[secondCompetitorIndex];

                    int firstCompetitorsScore = problem.EvaluateEquation(firstEquation);
                    int secondCompetitorsScore = problem.EvaluateEquation(secondEquation);
                    if (firstCompetitorsScore == secondCompetitorsScore)
                    {
                        continue;
                    }
                    else if (firstCompetitorsScore < secondCompetitorsScore)
                    {
                        secondEquation.CopyFrom(firstEquation);
                        if (firstCompetitorsScore < _bestScore)
                        {
                            _bestScore = firstCompetitorsScore;
                            _bestEquation = firstEquation.Copy();
                        }
                    }
                    else
                    {
                        firstEquation.CopyFrom(secondEquation);
                        if (secondCompetitorsScore < _bestScore)
                        {
                            _bestScore = secondCompetitorsScore;
                            _bestEquation = secondEquation.Copy();
                        }
                    }
                }

                int operatorCountToRandomize = (int)(_operatorCount * _candidateRandomizationRate);
                for (int i = 0; i < equations.Length; i++)
                {
                    RandomizeSmallPartOfEquation(random, equations[i], operatorCountToRandomize);
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
        return new RandomEvolutionSolver(_operatorCount, _candidateCount, _candidateCompetitionRate, _candidateRandomizationRate);
    }

    private static void RandomizeSmallPartOfEquation(Random random, ProblemEquation equation, int operatorCountToRandomize)
    {
        Span<NandOperator> operators = equation.NandOperators;
        int staticResultSize = equation.StaticResultSize;
        for (int i = 0; i < operatorCountToRandomize; i++)
        {
            int operatorIndex = random.Next(0, operators.Length);
            int leftValueIndex = random.Next(0, staticResultSize + operatorIndex);
            int rightValueIndex = random.Next(0, staticResultSize + operatorIndex);
            operators[operatorIndex] = new NandOperator(leftValueIndex, rightValueIndex);
        }
    }
}
