using System.Runtime.Intrinsics;

namespace Equation.Solver.Tests.Utilities;

internal sealed record ProblemParts(ProblemEquation Equation, EquationValues EquationValues, ProblemCollection ProblemCollection, EquationProblem EquationProblem)
{
    public ProblemExample[] Examples => ProblemCollection.Examples;

    public unsafe Vector256<int> GetVector(int index)
    {
        return ProblemCollection.Vectors[index];
    }
}
