namespace Equation.Solver.Solvers;

internal sealed class DefaultChunkSolverSolver : ISolver
{
    private readonly IChunkSolver _chunkSolver;
    private EquationScore? _bestScore;

    public DefaultChunkSolverSolver(IChunkSolver chunkSolver)
    {
        _chunkSolver = chunkSolver;
    }

    public SolverReport? GetReport()
    {
        var report = _chunkSolver.GetReport();
        _bestScore = report?.BestScore;
        return report;
    }

    public async Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        await _chunkSolver.PrepareToSolveAsync(problem, cancellationToken);

        _bestScore = EquationScore.MaxScore;
        while (_bestScore?.WrongBits != 0 && !cancellationToken.IsCancellationRequested)
        {
            await _chunkSolver.SolveStepAsync(problem, cancellationToken);
        }
    }

    public ISolver Copy()
    {
        throw new NotImplementedException();
    }
}
