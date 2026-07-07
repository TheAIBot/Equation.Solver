namespace Equation.Solver;

internal readonly struct ProblemInput
{
    public readonly int[] Indexes { get; }

    public ProblemInput(int[] indexes)
    {
        Indexes = indexes;
    }
}
