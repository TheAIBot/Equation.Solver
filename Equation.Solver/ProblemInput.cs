using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly record struct ProblemInput(Vector256<int>[] Inputs)
{
    public int Count => Inputs.Length;
}
