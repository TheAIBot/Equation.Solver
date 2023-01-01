using System.Threading.Tasks.Dataflow;

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

    public async Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        Random random = new Random();
        var parallelBlock = new TransformBlock<IChunkEvolver, IChunkEvolver>(x =>
        {
            x.EvolveChunk(problem);
            return x;
        }, new ExecutionDataflowBlockOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount - 1,
            SingleProducerConstrained = true
        });

        foreach (var chunk in _chunks)
        {
            await AddToBlock(parallelBlock, chunk, cancellationToken);
        }

        while (_chunks.Min(x => x.BestScore) > 0)
        {
            IChunkEvolver firstChunk = await GetRandomChunk(parallelBlock, random, cancellationToken);
            IChunkEvolver secondChunk = await GetRandomChunk(parallelBlock, random, cancellationToken);

            ScoredProblemEquation[] firstChunkEquations = firstChunk.Equations;
            ScoredProblemEquation[] secondChunkEquations = secondChunk.Equations;

            for (int i = 0; i < firstChunkEquations.Length; i++)
            {
                if (random.Next(0, 2) == 1)
                {
                    var temp = firstChunkEquations[i];
                    firstChunkEquations[i] = secondChunkEquations[i];
                    secondChunkEquations[i] = temp;
                }
            }

            firstChunk.UpdateBestEquation();
            secondChunk.UpdateBestEquation();

            await AddToBlock(parallelBlock, firstChunk, cancellationToken);
            await AddToBlock(parallelBlock, secondChunk, cancellationToken);
        }

        parallelBlock.Complete();
        await parallelBlock.Completion;
    }

    private async Task<IChunkEvolver> GetRandomChunk(TransformBlock<IChunkEvolver, IChunkEvolver> block, Random random, CancellationToken cancellationToken)
    {
        int chunkCounter = random.Next(0, _chunks.Length);
        while (chunkCounter < 0)
        {
            var chunk = await block.ReceiveAsync(cancellationToken);
            await AddToBlock(block, chunk, cancellationToken);
            chunkCounter--;
        }

        return await block.ReceiveAsync(cancellationToken);
    }

    private static async Task AddToBlock(TransformBlock<IChunkEvolver, IChunkEvolver> block, IChunkEvolver chunk, CancellationToken cancellationToken)
    {
        if (!await block.SendAsync(chunk, cancellationToken))
        {
            block.Complete();
            await block.Completion;
        }
    }

    public ISolver Copy()
    {
        return new RandomChunkEvolutionSolver(_chunks.Length, _chunks[0]);
    }
}
