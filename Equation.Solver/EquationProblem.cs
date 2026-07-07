using Equation.Solver.Score;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class EquationProblem
{
    private readonly ProblemCollection _problemCollection;
    public int ParameterCount => _problemCollection.Examples[0].Input.Indexes.Length;
    public int OutputCount => _problemCollection.Examples[0].Output.Outputs.Length;

    public EquationProblem(ProblemCollection problemCollection)
    {
        ArgumentOutOfRangeException.ThrowIfZero(problemCollection.Examples.Length);
        _problemCollection = problemCollection;
    }

    public SlimEquationScore EvaluateEquation(ProblemEquation equation, EquationValues equationValues)
    {
        Span<int> bitErrors = stackalloc int[OutputCount];
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
        if (bitErrors.Length != OutputCount)
        {
            throw new ArgumentException($"Must be the same length as {nameof(OutputCount)}", nameof(bitErrors));
        }

        for (int i = 0; i < _problemCollection.Examples.Length; i++)
        {
            ProblemExample example = _problemCollection.Examples[i];
            ReadOnlySpan<Vector256<int>> equationResult = equation.Calculate(equationValues, example, _problemCollection);
            example.Output.CalculateDifference(equationResult, bitErrors);
        }
    }

    public Vector256<int>[] GetEquationResults(ProblemEquation equation, EquationValues equationValues)
    {
        Vector256<int>[] outputResults = new Vector256<int>[_problemCollection.Examples.Length * equation.OutputSize];

        int outputResultIndex = 0;
        for (int i = 0; i < _problemCollection.Examples.Length; i++)
        {
            ProblemExample example = _problemCollection.Examples[i];
            ReadOnlySpan<Vector256<int>> equationResult = equation.Calculate(equationValues, example, _problemCollection);
            for (int z = 0; z < equationResult.Length; z++)
            {
                outputResults[outputResultIndex++] = equationResult[z];
            }
        }

        return outputResults;
    }

    public IEnumerable<bool> GetExampleCorrectness(ProblemEquation equation, EquationValues equationValues)
    {
        for (int i = 0; i < _problemCollection.Examples.Length; i++)
        {
            ReadOnlySpan<Vector256<int>> equationResult = equation.Calculate(equationValues, _problemCollection.Examples[i], _problemCollection);
            bool[] correctness = _problemCollection.Examples[i].Output.GetExampleCorrectness(equationResult);
            foreach (bool correct in correctness)
            {
                yield return correct;
            }
        }
    }
}
