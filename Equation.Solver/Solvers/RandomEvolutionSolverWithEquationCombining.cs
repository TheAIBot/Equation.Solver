using Equation.Solver.Evolvers;
using Equation.Solver.Score;
using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class RandomEvolutionSolverWithEquationCombining : ISolver, IChunkSolver
{
    private readonly int _parameterCount;
    private readonly int _operatorCount;
    private readonly int _outputCount;
    private readonly int _candidateCount;
    private readonly float _candidateCompetitionRate;
    private readonly int _candidateOperatorEvolveCount;
    private readonly float _candidateRandomEvolutionRate;
    private readonly float _candidateRandomCombiningRate;
    private readonly float _chanceOnlyMoveOperator;
    private readonly NandMover _nandMover;
    private readonly NandChangerOnlyUsedOperators _nandChanger;
    private readonly EquationCombiner _equationCombiner;
    private readonly FullScorer _fullScorer;
    private readonly EquationWithScore[] _familyEquationsWithScore = new EquationWithScore[3];
    private long _iterationCount;
    private EquationScore? _bestScore;
    [AllowNull]
    private ProblemEquation _bestEquation;
    private bool _isRunning = false;
    private Random? _random;
    private EquationWithScore[]? _equationsWithScore;
    private EquationValues? _equationValues;
    private HashSet<int> _usedOperations = [];

    public RandomEvolutionSolverWithEquationCombining(int parameterCount,
                                                      int operatorCount,
                                                      int outputCount,
                                                      int candidateCount,
                                                      float candidateCompetitionRate,
                                                      int candidateOperatorEvolveCount,
                                                      float candidateRandomEvolutionRate,
                                                      float candidateRandomCombiningRate,
                                                      float chanceOnlyMoveOperator)
    {
        _parameterCount = parameterCount;
        _operatorCount = operatorCount;
        _outputCount = outputCount;
        _candidateCount = candidateCount;
        _candidateCompetitionRate = candidateCompetitionRate;
        _candidateOperatorEvolveCount = candidateOperatorEvolveCount;
        _candidateRandomEvolutionRate = candidateRandomEvolutionRate;
        _candidateRandomCombiningRate = candidateRandomCombiningRate;
        _chanceOnlyMoveOperator = chanceOnlyMoveOperator;
        _nandMover = new NandMover(parameterCount, operatorCount);
        _nandChanger = new NandChangerOnlyUsedOperators();
        _equationCombiner = new EquationCombiner(operatorCount, outputCount);
        _fullScorer = new FullScorer();
    }

    public SolverReport? GetReport()
    {
        if (!_isRunning)
        {
            return null;
        }

        var bestScore = _bestScore;
        var bestEquation = _bestEquation;
        if (bestScore == null ||
            bestEquation == null)
        {
            return null;
        }
        return new SolverReport(_iterationCount, bestScore.Value, bestEquation);
    }

    public async Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        try
        {
            await PrepareToSolveAsync(problem, cancellationToken);

            while (_bestScore?.WrongBits != 0 && !cancellationToken.IsCancellationRequested)
            {
                await SolveStepAsync(problem, cancellationToken);
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    public Task PrepareToSolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        _random = new Random();
        _equationsWithScore = new EquationWithScore[_candidateCount];
        _equationValues = new EquationValues(problem.ParameterCount, _operatorCount);

        var random = _random;
        var equationsWithScore = _equationsWithScore;
        var equationValues = _equationValues;
        for (int i = 0; i < equationsWithScore.Length; i++)
        {
            equationsWithScore[i] = new EquationWithScore(new ProblemEquation(_operatorCount, problem.OutputCount), null);
            RandomSolver.Randomize(random, equationsWithScore[i].Equation, equationValues);
        }

        _isRunning = true;
        _iterationCount = 0;
        _bestScore = EquationScore.MaxScore;

        return Task.CompletedTask;
    }

    public Task SolveStepAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        if (_random == null ||
            _equationsWithScore == null ||
            _equationValues == null)
        {
            throw new InvalidOperationException();
        }

        var random = _random;
        var equationsWithScore = _equationsWithScore;
        var equationValues = _equationValues;
        var usedOperations = _usedOperations;

        var familyEquationsWithScore = _familyEquationsWithScore;

        int competitionCount = (int)(_candidateCount * _candidateCompetitionRate);
        for (int i = 0; i < competitionCount; i++)
        {
            int firstCompetitorIndex = random.Next(0, equationsWithScore.Length);
            int secondCompetitorIndex = random.Next(0, equationsWithScore.Length);
            ref EquationWithScore firstEquationWithScore = ref equationsWithScore[firstCompetitorIndex];
            ref EquationWithScore secondEquationWithScore = ref equationsWithScore[secondCompetitorIndex];

            firstEquationWithScore.Score ??= problem.EvaluateEquation(firstEquationWithScore.Equation, equationValues);
            secondEquationWithScore.Score ??= problem.EvaluateEquation(secondEquationWithScore.Equation, equationValues);
            if (firstEquationWithScore.Score == secondEquationWithScore.Score)
            {
                continue;
            }
            else if (firstEquationWithScore.Score < secondEquationWithScore.Score)
            {
                ReplaceWorseEquationWithBetterEquationAndEvolve(random, equationValues, ref firstEquationWithScore, ref secondEquationWithScore);
            }
            else
            {
                ReplaceWorseEquationWithBetterEquationAndEvolve(random, equationValues, ref secondEquationWithScore, ref firstEquationWithScore);
            }
        }
        _iterationCount += competitionCount;

        int randomEvolutionCount = (int)(_candidateCount * _candidateRandomEvolutionRate);
        for (int i = 0; i < randomEvolutionCount; i++)
        {
            int equationIndex = random.Next(equationsWithScore.Length);
            ref EquationWithScore equationWithScore = ref equationsWithScore[equationIndex];

            Evolve(random, equationValues, ref equationWithScore);
        }
        _iterationCount += randomEvolutionCount;

        usedOperations.Clear();
        int randomCombiningCount = (int)(_candidateCount * _candidateRandomCombiningRate);
        for (int i = 0; i < randomCombiningCount; i++)
        {
            int firstCompetitorIndex = GetUnusedEquation(random, equationsWithScore, usedOperations);
            int secondCompetitorIndex = GetUnusedEquation(random, equationsWithScore, usedOperations);
            int thirdCompetitorIndex = GetUnusedEquation(random, equationsWithScore, usedOperations);

            ref EquationWithScore firstEquationWithScore = ref equationsWithScore[firstCompetitorIndex];
            ref EquationWithScore secondEquationWithScore = ref equationsWithScore[secondCompetitorIndex];
            ref EquationWithScore thirdEquationWithScore = ref equationsWithScore[thirdCompetitorIndex];

            firstEquationWithScore.Score ??= problem.EvaluateEquation(firstEquationWithScore.Equation, equationValues);
            secondEquationWithScore.Score ??= problem.EvaluateEquation(secondEquationWithScore.Equation, equationValues);
            thirdEquationWithScore.Score ??= problem.EvaluateEquation(thirdEquationWithScore.Equation, equationValues);

            familyEquationsWithScore[0] = firstEquationWithScore;
            familyEquationsWithScore[1] = secondEquationWithScore;
            familyEquationsWithScore[2] = thirdEquationWithScore;
            Array.Sort(familyEquationsWithScore);

            if (!_equationCombiner.CombineEquations(random,
                                                    equationValues.InputParameterCount,
                                                    familyEquationsWithScore[0].Equation,
                                                    familyEquationsWithScore[1].Equation,
                                                    familyEquationsWithScore[2].Equation))
            {
                continue;
            }

            if (firstEquationWithScore.Equation == familyEquationsWithScore[2].Equation)
            {
                firstEquationWithScore.Score = null;
            }
            else if (secondEquationWithScore.Equation == familyEquationsWithScore[2].Equation)
            {
                secondEquationWithScore.Score = null;
            }
            else
            {
                thirdEquationWithScore.Score = null;
            }
        }
        _iterationCount += randomCombiningCount;
        return Task.CompletedTask;
    }

    public ISolver Copy()
    {
        return new RandomEvolutionSolverWithEquationCombining(_parameterCount,
                                                              _operatorCount,
                                                              _outputCount,
                                                              _candidateCount,
                                                              _candidateCompetitionRate,
                                                              _candidateOperatorEvolveCount,
                                                              _candidateRandomEvolutionRate,
                                                              _candidateRandomCombiningRate,
                                                              _chanceOnlyMoveOperator);
    }

    public IChunkSolver CopyChunkSolver()
    {
        return new RandomEvolutionSolverWithEquationCombining(_parameterCount,
                                                              _operatorCount,
                                                              _outputCount,
                                                              _candidateCount,
                                                              _candidateCompetitionRate,
                                                              _candidateOperatorEvolveCount,
                                                              _candidateRandomEvolutionRate,
                                                              _candidateRandomCombiningRate,
                                                              _chanceOnlyMoveOperator);
    }

    public EquationWithScore[] GetEquations()
    {
        if (_equationsWithScore == null)
        {
            throw new InvalidOperationException("Equations have not been initialized yet.");
        }

        return _equationsWithScore;
    }

    public void UpdateInternalStateAfterEquationChanges()
    {
        if (_equationsWithScore == null)
        {
            throw new InvalidOperationException();
        }

    }

    private void ReplaceWorseEquationWithBetterEquationAndEvolve(Random random,
                                                                 EquationValues equationValues,
                                                                 ref EquationWithScore betterEquationWithScore,
                                                                 ref EquationWithScore worseEquationWithScore)
    {
        worseEquationWithScore.Equation.CopyFrom(betterEquationWithScore.Equation);
        worseEquationWithScore.Score = betterEquationWithScore.Score;

        Evolve(random, equationValues, ref worseEquationWithScore);

        if (betterEquationWithScore.Score < _bestScore)
        {
            _bestScore = _fullScorer.ToFullScore(betterEquationWithScore.Score.Value, equationValues, betterEquationWithScore.Equation);
            _bestEquation = betterEquationWithScore.Equation.Copy();
        }
    }

    private void Evolve(Random random, EquationValues equationValues, ref EquationWithScore equationWithScore)
    {
        if (random.NextSingle() < _chanceOnlyMoveOperator)
        {
            _nandMover.MoveRandomNandOperator(random,
                                              equationValues.InputParameterCount,
                                              equationWithScore.Equation.OutputSize,
                                              equationWithScore.Equation.NandOperators,
                                              equationWithScore.Equation.OperatorsUsed);
        }
        else
        {
            int operatorCountToRandomize = random.Next(1, _candidateOperatorEvolveCount + 1);
            if (_nandChanger.RandomizeSmallPartOfEquation(random, equationWithScore.Equation, equationValues, operatorCountToRandomize))
            {
                equationWithScore.Score = null;
            }
        }
    }

    private int GetUnusedEquation(Random random, EquationWithScore[] equationsWithScore, HashSet<int> usedEquations)
    {
        int index;
        do
        {
            index = random.Next(equationsWithScore.Length);
        } while (usedEquations.Contains(index));

        usedEquations.Add(index);
        return index;
    }
}

internal record struct EquationWithScore(ProblemEquation Equation, SlimEquationScore? Score) : IComparable<EquationWithScore>
{
    public int CompareTo(EquationWithScore other)
    {
        return Score!.Value.WrongBits.CompareTo(other.Score!.Value.WrongBits);
    }
}
