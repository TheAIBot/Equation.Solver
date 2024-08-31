﻿using Equation.Solver.Evolvers;
using Equation.Solver.Score;
using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class RandomEvolutionSolverWithEquationCombining : ISolver
{
    private readonly int _parameterCount;
    private readonly int _operatorCount;
    private readonly int _outputCount;
    private readonly int _candidateCount;
    private readonly float _candidateCompetitionRate;
    private readonly float _candidateRandomizationRate;
    private readonly float _candidateRandomEvolutionRate;
    private readonly float _candidateRandomCombiningRate;
    private readonly float _chanceOnlyMoveOperator;
    private readonly NandMover _nandMover;
    private readonly NandChanger _nandChanger;
    private readonly EquationCombiner _equationCombiner;
    private readonly FullScorer _fullScorer;
    private long _iterationCount;
    private EquationScore? _bestScore;
    [AllowNull]
    private ProblemEquation _bestEquation;
    private bool _isRunning = false;

    public RandomEvolutionSolverWithEquationCombining(int parameterCount,
                                                      int operatorCount,
                                                      int outputCount,
                                                      int candidateCount,
                                                      float candidateCompetitionRate,
                                                      float candidateRandomizationRate,
                                                      float candidateRandomEvolutionRate,
                                                      float candidateRandomCombiningRate,
                                                      float chanceOnlyMoveOperator)
    {
        _parameterCount = parameterCount;
        _operatorCount = operatorCount;
        _outputCount = outputCount;
        _candidateCount = candidateCount;
        _candidateCompetitionRate = candidateCompetitionRate;
        _candidateRandomizationRate = candidateRandomizationRate;
        _candidateRandomEvolutionRate = candidateRandomEvolutionRate;
        _candidateRandomCombiningRate = candidateRandomCombiningRate;
        _chanceOnlyMoveOperator = chanceOnlyMoveOperator;
        _nandMover = new NandMover(parameterCount, operatorCount);
        _nandChanger = new NandChanger();
        _equationCombiner = new EquationCombiner(operatorCount, outputCount);
        _fullScorer = new FullScorer();
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
            var equationsWithScore = new EquationWithScore[_candidateCount];
            var equationValues = new EquationValues(problem.ParameterCount, _operatorCount);
            for (int i = 0; i < equationsWithScore.Length; i++)
            {
                equationsWithScore[i] = new EquationWithScore(new ProblemEquation(_operatorCount, problem.OutputCount), null);
                RandomSolver.Randomize(random, equationsWithScore[i].Equation, equationValues);
            }

            var familyEquationsWithScore = new EquationWithScore[3];

            var usedOperations = new HashSet<int>();
            _iterationCount = 0;
            _bestScore = EquationScore.MaxScore;
            while (_bestScore?.WrongBits != 0 && !cancellationToken.IsCancellationRequested)
            {
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
                                                       equationValues.StaticResultSize,
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
        return new RandomEvolutionSolverWithEquationCombining(_parameterCount,
                                                              _operatorCount,
                                                              _outputCount,
                                                              _candidateCount,
                                                              _candidateCompetitionRate,
                                                              _candidateRandomizationRate,
                                                              _candidateRandomEvolutionRate,
                                                              _candidateRandomCombiningRate,
                                                              _chanceOnlyMoveOperator);
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
                                              equationValues.StaticResultSize,
                                              equationWithScore.Equation.OutputSize,
                                              equationWithScore.Equation.NandOperators,
                                              equationWithScore.Equation.OperatorsUsed);
        }
        else
        {
            int operatorCountToRandomize = (int)(_operatorCount * _candidateRandomizationRate);
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

    private record struct EquationWithScore(ProblemEquation Equation, SlimEquationScore? Score) : IComparable<EquationWithScore>
    {
        public int CompareTo(EquationWithScore other)
        {
            return Score!.Value.WrongBits - other.Score!.Value.WrongBits;
        }
    }
}