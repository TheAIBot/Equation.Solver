namespace Equation.Solver.Solvers;

internal interface ISolver
{
    SolverReport? GetReport();
    Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken);

    ISolver Copy();
}
