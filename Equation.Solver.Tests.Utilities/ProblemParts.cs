namespace Equation.Solver.Tests.Utilities;

internal sealed record ProblemParts(ProblemEquation Equation, EquationValues EquationValues, ProblemCollection ProblemCollection, EquationProblem EquationProblem)
{
    public ProblemExample[] Examples => ProblemCollection.Examples;
}
