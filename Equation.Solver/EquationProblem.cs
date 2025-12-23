using Equation.Solver.Score;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class EquationProblem
{
    private readonly ProblemExample[] _examples;
    public int ParameterCount => _examples[0].Input.Inputs.Length;
    public int OutputCount => _examples[0].Output.Outputs.Length;

    public EquationProblem(ProblemExample[] examples)
    {
        _examples = examples;
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

        for (int i = 0; i < _examples.Length; i++)
        {
            ProblemExample example = _examples[i];
            ReadOnlySpan<Vector256<int>> equationResult = equation.Calculate(equationValues, example);
            example.Output.CalculateDifference(equationResult, bitErrors);
        }
    }

    public Vector256<int>[] GetEquationResults(ProblemEquation equation, EquationValues equationValues)
    {
        Vector256<int>[] outputResults = new Vector256<int>[_examples.Length * equation.OutputSize];

        int outputResultIndex = 0;
        for (int i = 0; i < _examples.Length; i++)
        {
            ProblemExample example = _examples[i];
            ReadOnlySpan<Vector256<int>> equationResult = equation.Calculate(equationValues, example);
            for (int z = 0; z < equationResult.Length; z++)
            {
                outputResults[outputResultIndex++] = equationResult[z];
            }
        }

        return outputResults;
    }
}
