using System.Runtime.Intrinsics;
using Equation.Solver.Score;

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
        int score = 0;
        for (int i = 0; i < _examples.Length; i++)
        {
            ProblemExample example = _examples[i];
            equationValues.SetParameters(example.Input);
            ReadOnlySpan<Vector256<int>> equationResult = equation.Calculate(equationValues);
            score += example.Output.CalculateDifference(equationResult);
        }

        return new SlimEquationScore(score);
    }
}
