using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class RandomChunkEvolver : IChunkEvolver
{
    private readonly int _operatorCount;
    private readonly int _candidateCount;
    private readonly float _candidateCompetitionRate;
    private readonly float _candidateRandomizationRate;
    private readonly int _parameterCount;
    private readonly int _outputCount;
    private readonly Random _random;
    private readonly ScoredProblemEquation[] _equations;
    private readonly EquationValues _equationValues;
    private long _iterationCount = 0;
    private EquationScore _bestScore = EquationScore.MaxScore;
    [AllowNull]
    private ProblemEquation _bestEquation;
    public ScoredProblemEquation[] Equations => _equations;
    public EquationScore BestScore => _bestScore;

    public RandomChunkEvolver(int operatorCount, int candidateCount, float candidateCompetitionRate, float candidateRandomizationRate, int parameterCount, int outputCount)
        : this(operatorCount, candidateCount, candidateCompetitionRate, candidateRandomizationRate, parameterCount, outputCount, new Random())
    {
    }

    private RandomChunkEvolver(int operatorCount, int candidateCount, float candidateCompetitionRate, float candidateRandomizationRate, int parameterCount, int outputCount, Random random)
    {
        _operatorCount = operatorCount;
        _candidateCount = candidateCount;
        _candidateCompetitionRate = candidateCompetitionRate;
        _candidateRandomizationRate = candidateRandomizationRate;
        _parameterCount = parameterCount;
        _outputCount = outputCount;

        _random = random;
        _equations = new ScoredProblemEquation[_candidateCount];
        _equationValues = new EquationValues(parameterCount, _operatorCount);
        for (int i = 0; i < _equations.Length; i++)
        {
            _equations[i] = new ScoredProblemEquation(EquationScore.MaxScore.ToSlimScore(), new ProblemEquation(_operatorCount, outputCount));
            RandomSolver.Randomize(_random, _equations[i].Equation, _equationValues);
        }
    }

    public SolverReport? GetReport()
    {
        if (_bestScore == EquationScore.MaxScore)
        {
            return null;
        }
        return new SolverReport(_iterationCount, _bestScore, _bestEquation);
    }

    public void EvolveChunk(EquationProblem problem)
    {
        _iterationCount += _candidateCount;

        int operatorCountToRandomize = (int)(_operatorCount * _candidateRandomizationRate);
        for (int i = 0; i < _equations.Length; i++)
        {
            RandomizeSmallPartOfEquation(_random, _equations[i].Equation, _equationValues, operatorCountToRandomize);
        }

        if (_bestEquation != null)
        {
            int bestEquationInsertIndex = _random.Next(0, _equations.Length);
            _equations[bestEquationInsertIndex] = new ScoredProblemEquation(_bestScore.ToSlimScore(), _bestEquation);
        }

        int competitionCount = (int)(_candidateCount * _candidateCompetitionRate);
        for (int i = 0; i < competitionCount; i++)
        {
            int firstCompetitorIndex = _random.Next(0, _equations.Length);
            int secondCompetitorIndex = _random.Next(0, _equations.Length);
            ref ScoredProblemEquation firstEquation = ref _equations[firstCompetitorIndex];
            ref ScoredProblemEquation secondEquation = ref _equations[secondCompetitorIndex];

            SlimEquationScore firstCompetitorsScore = problem.EvaluateEquation(firstEquation.Equation, _equationValues);
            SlimEquationScore secondCompetitorsScore = problem.EvaluateEquation(secondEquation.Equation, _equationValues);

            firstEquation.Score = firstCompetitorsScore;
            secondEquation.Score = secondCompetitorsScore;

            if (firstCompetitorsScore == secondCompetitorsScore)
            {
                continue;
            }
            else if (firstCompetitorsScore < secondCompetitorsScore)
            {
                secondEquation.Equation.CopyFrom(firstEquation.Equation);
                if (firstCompetitorsScore < _bestScore)
                {
                    _bestScore = firstCompetitorsScore.ToFullScore(_equationValues, firstEquation.Equation);
                    _bestEquation = firstEquation.Equation.Copy();
                }
            }
            else
            {
                firstEquation.Equation.CopyFrom(secondEquation.Equation);
                if (secondCompetitorsScore < _bestScore)
                {
                    _bestScore = secondCompetitorsScore.ToFullScore(_equationValues, secondEquation.Equation);
                    _bestEquation = secondEquation.Equation.Copy();
                }
            }
        }
    }

    public void UpdateBestEquation()
    {
        ScoredProblemEquation bestEquation = _equations.MinBy(x => x.Score);
        _bestScore = bestEquation.Score.ToFullScore(_equationValues, bestEquation.Equation);
        _bestEquation = bestEquation.Equation;
    }

    public IChunkEvolver Copy(int randomSeed)
    {
        return new RandomChunkEvolver(_operatorCount, _candidateCount, _candidateCompetitionRate, _candidateRandomizationRate, _parameterCount, _outputCount, new Random(randomSeed));
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
