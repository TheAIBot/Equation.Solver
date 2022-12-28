namespace Equation.Solver.Solvers;

internal interface ISolver : IReporting
{
    Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken);

    ISolver Copy();
}