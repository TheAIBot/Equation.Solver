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

    public int EvaluateEquation(ProblemEquation equation)
    {
        int score = 0;
        for (int i = 0; i < _examples.Length; i++)
        {
            ProblemExample example = _examples[i];
            ReadOnlySpan<int> equationResult = equation.Calculate(example.Input.Inputs);
            score += example.Output.CalculateDifference(equationResult, example.ExampleCount);
        }

        return score;
    }
}
