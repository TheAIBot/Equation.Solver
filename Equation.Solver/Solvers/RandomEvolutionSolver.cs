using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class RandomEvolutionSolver : ISolver
{
    private readonly int _parameterCount;
    private readonly int _operatorCount;
    private readonly int _candidateCount;
    private readonly float _candidateCompetitionRate;
    private readonly float _candidateRandomizationRate;
    private readonly float _chanceLoserOverriddenByWinner;
    private readonly float _chanceOnlyMoveOperator;
    private readonly NandMover _nandMover;
    private long _iterationCount;
    private EquationScore? _bestScore;
    [AllowNull]
    private ProblemEquation _bestEquation;
    private bool _isRunning = false;

    public RandomEvolutionSolver(int parameterCount,
                                 int operatorCount,
                                 int candidateCount,
                                 float candidateCompetitionRate,
                                 float candidateRandomizationRate,
                                 float chanceLoserOverriddenByWinner,
                                 float chanceOnlyMoveOperator)
    {
        _parameterCount = parameterCount;
        _operatorCount = operatorCount;
        _candidateCount = candidateCount;
        _candidateCompetitionRate = candidateCompetitionRate;
        _candidateRandomizationRate = candidateRandomizationRate;
        _chanceLoserOverriddenByWinner = chanceLoserOverriddenByWinner;
        _chanceOnlyMoveOperator = chanceOnlyMoveOperator;
        _nandMover = new NandMover(parameterCount, operatorCount);
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
            var equationValues = new EquationValues(problem.ParameterCount, _operatorCount);
            for (int i = 0; i < equations.Length; i++)
            {
                equations[i] = new ProblemEquation(_operatorCount, problem.OutputCount);
                RandomSolver.Randomize(random, equations[i], equationValues);
            }

            _iterationCount = 0;
            _bestScore = EquationScore.MaxScore;
            while (_bestScore?.WrongBits != 0 && !cancellationToken.IsCancellationRequested)
            {
                _iterationCount += _candidateCount;
                int competitionCount = (int)(_candidateCount * _candidateCompetitionRate);
                for (int i = 0; i < competitionCount; i++)
                {
                    int firstCompetitorIndex = random.Next(0, equations.Length);
                    int secondCompetitorIndex = random.Next(0, equations.Length);
                    ProblemEquation firstEquation = equations[firstCompetitorIndex];
                    ProblemEquation secondEquation = equations[secondCompetitorIndex];

                    SlimEquationScore firstCompetitorsScore = problem.EvaluateEquation(firstEquation, equationValues);
                    SlimEquationScore secondCompetitorsScore = problem.EvaluateEquation(secondEquation, equationValues);
                    if (firstCompetitorsScore == secondCompetitorsScore)
                    {
                        continue;
                    }
                    else if (_chanceLoserOverriddenByWinner < random.NextSingle())
                    {
                        continue;
                    }
                    else if (firstCompetitorsScore < secondCompetitorsScore)
                    {
                        secondEquation.CopyFrom(firstEquation);
                        if (firstCompetitorsScore < _bestScore)
                        {
                            _bestScore = firstCompetitorsScore.ToFullScore(equationValues, firstEquation);
                            _bestEquation = firstEquation.Copy();
                        }
                    }
                    else
                    {
                        firstEquation.CopyFrom(secondEquation);
                        if (secondCompetitorsScore < _bestScore)
                        {
                            _bestScore = secondCompetitorsScore.ToFullScore(equationValues, secondEquation);
                            _bestEquation = secondEquation.Copy();
                        }
                    }
                }

                int operatorCountToRandomize = (int)(_operatorCount * _candidateRandomizationRate);
                for (int i = 0; i < equations.Length; i++)
                {
                    if (random.NextSingle() < _chanceOnlyMoveOperator)
                    {
                        _nandMover.MoveRandomNandOperator(random,
                                                          equationValues.StaticResultSize,
                                                          equations[i].OutputSize,
                                                          equations[i].NandOperators);
                    }
                    else
                    {
                        RandomizeSmallPartOfEquation(random, equations[i], equationValues, operatorCountToRandomize);
                    }
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
        return new RandomEvolutionSolver(_parameterCount,
                                         _operatorCount,
                                         _candidateCount,
                                         _candidateCompetitionRate,
                                         _candidateRandomizationRate,
                                         _chanceLoserOverriddenByWinner,
                                         _chanceOnlyMoveOperator);
    }

    private static void RandomizeSmallPartOfEquation(Random random, ProblemEquation equation, EquationValues equationValues, int operatorCountToRandomize)
    {
        Span<NandOperator> operators = equation.NandOperators;
        int staticResultSize = equationValues.StaticResultSize;
        for (int i = 0; i < operatorCountToRandomize; i++)
        {
            int operatorIndex = random.Next(0, operators.Length);
            int leftValueIndex = random.Next(0, staticResultSize + operatorIndex);
            int rightValueIndex = random.Next(0, staticResultSize + operatorIndex);
            operators[operatorIndex] = new NandOperator(leftValueIndex, rightValueIndex);
        }
    }
}
