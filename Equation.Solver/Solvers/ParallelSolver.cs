namespace Equation.Solver.Solvers;

internal sealed class ParallelSolver : ISolver
{
    private readonly ISolver[] _solvers;

    public ParallelSolver(ISolver solver)
    {
        _solvers = Enumerable.Range(0, Environment.ProcessorCount - 2)
                             .Select(_ => solver.Copy())
                             .ToArray();
    }

    private ParallelSolver(ISolver[] solvers)
    {
        _solvers = solvers;
    }

    public SolverReport? GetReport()
    {
        SolverReport[] reports = GetAllReports();
        if (reports.Length == 0)
        {
            return null;
        }
        SolverReport bestScoreReport = reports.MinBy(x => x.BestScore) ?? throw new InvalidOperationException("No best report was found");

        return new SolverReport(reports.Sum(x => x.IterationCount), bestScoreReport.BestScore, bestScoreReport.BestEquation);
    }


    public SolverReport[] GetAllReports()
    {
        return _solvers.Select(solver => solver.GetReport())
                       .OfType<SolverReport>()
                       .ToArray();
    }
    public Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        return Task.WhenAll(_solvers.Select(x => Task.Run(() => x.SolveAsync(problem, cancellationToken))));
    }

    public ISolver Copy()
    {
        return new ParallelSolver(_solvers.Select(x => x.Copy()).ToArray());
    }
}
