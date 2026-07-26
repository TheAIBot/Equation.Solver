using Equation.Solver.Score;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class EquationProblem
{
    private readonly ProblemCollection _problemCollection;
    public int ParameterCount => _problemCollection.Examples[0].InputCount;
    public int OutputCount => _problemCollection.Examples[0].OutputCount;

    public EquationProblem(ProblemCollection problemCollection)
    {
        ArgumentOutOfRangeException.ThrowIfZero(problemCollection.Examples.Length);
        _problemCollection = problemCollection;
    }

    public SlimEquationScore EvaluateEquation(ProblemEquation equation, EquationValues equationValues)
    {
        Span<int> bitErrors = stackalloc int[OutputCount * _problemCollection.MaxBatchSize];
        EvaluateEquation(equation, equationValues, bitErrors);

        int score = 0;
        for (int i = 0; i < bitErrors.Length; i++)
        {
            score += bitErrors[i];
        }

        return new SlimEquationScore(score);
    }

    public void EvaluateEquation(ProblemEquation equation, EquationValues equationValues, Span<int> bitErrors)
    {
        if (bitErrors.Length != OutputCount * _problemCollection.MaxBatchSize)
        {
            throw new ArgumentException($"Must be the same length as {nameof(OutputCount)}", nameof(bitErrors));
        }

        for (int i = 0; i < _problemCollection.Examples.Length; i++)
        {
            ProblemExampleBatch example = _problemCollection.Examples[i];
            ReadOnlySpan<Vector256<int>> equationResult = equation.CalculateBatch(equationValues, example, _problemCollection);
            example.CalculateDifference(equationResult, bitErrors.Slice(0, example.OutputIndexes.Length), _problemCollection);
        }
    }

    public Vector256<int>[] GetEquationResults(ProblemEquation equation, EquationValues equationValues)
    {
        Vector256<int>[] outputResults = new Vector256<int>[_problemCollection.Examples.Sum(x => x.BatchSize) * equation.OutputSize];

        int outputResultIndex = 0;
        for (int exampleIndex = 0; exampleIndex < _problemCollection.Examples.Length; exampleIndex++)
        {
            ProblemExampleBatch example = _problemCollection.Examples[exampleIndex];
            ReadOnlySpan<Vector256<int>> equationResult = equation.CalculateBatch(equationValues, example, _problemCollection);
            for (int batchIndex = 0; batchIndex < example.BatchSize; batchIndex++)
            {
                for (int outputIndex = 0; outputIndex < example.OutputCount; outputIndex++)
                {
                    outputResults[outputResultIndex++] = equationResult[outputIndex * example.BatchSize + batchIndex];
                }
            }
        }

        return outputResults;
    }

    public IEnumerable<bool> GetExampleCorrectness(ProblemEquation equation, EquationValues equationValues)
    {
        for (int i = 0; i < _problemCollection.Examples.Length; i++)
        {
            ReadOnlySpan<Vector256<int>> equationResult = equation.CalculateBatch(equationValues, _problemCollection.Examples[i], _problemCollection);
            bool[] correctness = _problemCollection.Examples[i].GetExampleCorrectness(equationResult, _problemCollection);
            foreach (bool correct in correctness)
            {
                yield return correct;
            }
        }
    }
}
