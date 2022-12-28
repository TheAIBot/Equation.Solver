namespace Equation.Solver.Solvers;

internal sealed class RandomChunkEvolutionSolver : ISolver, IMultipleReporting
{
    private readonly IChunkEvolver[] _chunks;

    public RandomChunkEvolutionSolver(int chunkCount, IChunkEvolver chunkEvolver)
    {
        _chunks = Enumerable.Range(0, chunkCount)
                            .Select(_ => chunkEvolver.Copy())
                            .ToArray();
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
        return _chunks.Select(solver => solver.GetReport())
                      .OfType<SolverReport>()
                      .ToArray();
    }

    public Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        Random random = new Random();
        ParallelOptions parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount - 1
        };

        while (_chunks.Min(x => x.BestScore) > 0)
        {
            Parallel.ForEach(_chunks, parallelOptions, x => x.EvolveChunk(problem));

            Span<ScoredProblemEquation> firstChunkEquations = _chunks[random.Next(0, _chunks.Length)].Equations;
            Span<ScoredProblemEquation> secondChunkEquations = _chunks[random.Next(0, _chunks.Length)].Equations;

            for (int i = 0; i < firstChunkEquations.Length; i++)
            {
                if (random.Next(0, 2) == 1)
                {
                    var temp = firstChunkEquations[i];
                    firstChunkEquations[i] = secondChunkEquations[i];
                    secondChunkEquations[i] = temp;
                }
            }
        }

        return Task.CompletedTask;
    }

    public ISolver Copy()
    {
        return new RandomChunkEvolutionSolver(_chunks.Length, _chunks[0]);
    }
}
