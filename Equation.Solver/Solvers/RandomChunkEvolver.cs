﻿using System.Diagnostics.CodeAnalysis;

namespace Equation.Solver.Solvers;

internal sealed class RandomChunkEvolver : IChunkEvolver
{
    private readonly int _operatorCount;
    private readonly int _candidateCount;
    private readonly float _candidateCompetitionRate;
    private readonly float _candidateRandomizationRate;
    private readonly int _parameterCount;
    private readonly int _outputCount;
    private readonly Random _random = new Random();
    private readonly ScoredProblemEquation[] _equations;
    private long _iterationCount = 0;
    private int _bestScore = int.MaxValue;
    [AllowNull]
    private ProblemEquation _bestEquation;
    public Span<ScoredProblemEquation> Equations => _equations;
    public int BestScore => _bestScore;

    public RandomChunkEvolver(int operatorCount, int candidateCount, float candidateCompetitionRate, float candidateRandomizationRate, int parameterCount, int outputCount)
    {
        _operatorCount = operatorCount;
        _candidateCount = candidateCount;
        _candidateCompetitionRate = candidateCompetitionRate;
        _candidateRandomizationRate = candidateRandomizationRate;
        _parameterCount = parameterCount;
        _outputCount = outputCount;

        _random = new Random();
        _equations = new ScoredProblemEquation[_candidateCount];
        for (int i = 0; i < _equations.Length; i++)
        {
            _equations[i] = new ScoredProblemEquation(int.MaxValue, new ProblemEquation(parameterCount, _operatorCount, outputCount));
            RandomSolver.Randomize(_random, _equations[i].Equation);
        }
    }

    public SolverReport? GetReport()
    {
        if (_bestScore == int.MaxValue)
        {
            return null;
        }
        return new SolverReport(_iterationCount, _bestScore, _bestEquation);
    }

    public void EvolveChunk(EquationProblem problem)
    {
        _iterationCount += _candidateCount;
        int competitionCount = (int)(_candidateCount * _candidateCompetitionRate);
        for (int i = 0; i < competitionCount; i++)
        {
            int firstCompetitorIndex = _random.Next(0, _equations.Length);
            int secondCompetitorIndex = _random.Next(0, _equations.Length);
            ref ScoredProblemEquation firstEquation = ref _equations[firstCompetitorIndex];
            ref ScoredProblemEquation secondEquation = ref _equations[secondCompetitorIndex];

            int firstCompetitorsScore = problem.EvaluateEquation(firstEquation.Equation);
            int secondCompetitorsScore = problem.EvaluateEquation(secondEquation.Equation);

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
                    _bestScore = firstCompetitorsScore;
                    _bestEquation = firstEquation.Equation.Copy();
                }
            }
            else
            {
                firstEquation.Equation.CopyFrom(secondEquation.Equation);
                if (secondCompetitorsScore < _bestScore)
                {
                    _bestScore = secondCompetitorsScore;
                    _bestEquation = secondEquation.Equation.Copy();
                }
            }
        }

        int operatorCountToRandomize = (int)(_operatorCount * _candidateRandomizationRate);
        for (int i = 0; i < _equations.Length; i++)
        {
            RandomizeSmallPartOfEquation(_random, _equations[i].Equation, operatorCountToRandomize);
        }

        int bestEquationInsertIndex = _random.Next(0, _equations.Length);
        _equations[bestEquationInsertIndex] = new ScoredProblemEquation(_bestScore, _bestEquation);
    }

    public void UpdateBestEquation()
    {
        ScoredProblemEquation bestEquation = _equations.MinBy(x => x.Score);
        _bestScore = bestEquation.Score;
        _bestEquation = bestEquation.Equation;
    }

    public IChunkEvolver Copy()
    {
        return new RandomChunkEvolver(_operatorCount, _candidateCount, _candidateCompetitionRate, _candidateRandomizationRate, _parameterCount, _outputCount);
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