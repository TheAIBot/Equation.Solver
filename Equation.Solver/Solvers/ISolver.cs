namespace Equation.Solver.Solvers;

internal interface ISolver : IReporting
{
    Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken);

    ISolver Copy();
}

internal interface IChunkSolver : IReporting
{
    Task PrepareToSolveAsync(EquationProblem problem, CancellationToken cancellationToken);

    Task SolveStepAsync(EquationProblem problem, CancellationToken cancellationToken);

    IChunkSolver CopyChunkSolver(int chunkIndex);

    EquationWithScore[] GetEquations();

    void UpdateInternalStateAfterEquationChanges();
}