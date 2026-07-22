using Equation.Solver.Score;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace Equation.Solver.Solvers;

internal sealed class ParallelMixSolver : ISolver
{
    private readonly IChunkSolver[] _chunkSolvers;
    private readonly EquationProblem[] _problems;
    private readonly EquationProblem _wholeProblem;
    private readonly int _chunkSolverIterationsPerMix;
    private readonly float _chanceToMix;
    private readonly EquationValues _equationValues;
    private readonly FullScorer _fullScorer;
    private EquationScore? _bestScore;
    private Dictionary<int, (int ReportIteration, SolverReport Report, SlimEquationScore SlimScore)>? _solverIdToReportData;

    public ParallelMixSolver(IChunkSolver chunkSolver,
                             EquationProblem[] problems,
                             EquationProblem wholeProblem,
                             int operatorCount,
                             int chunkSolverIterationsPerMix,
                             float chanceToMix)
    {
        _chunkSolvers = problems.Select((_, i) => chunkSolver.CopyChunkSolver(i))
                                        .ToArray();
        _problems = problems;
        _wholeProblem = wholeProblem;
        _chunkSolverIterationsPerMix = chunkSolverIterationsPerMix;
        _chanceToMix = chanceToMix;
        _equationValues = new EquationValues(wholeProblem.ParameterCount, operatorCount);
        _fullScorer = new FullScorer(operatorCount);
    }

    public SolverReport? GetReport()
    {
        SolverReport[] allReports = _chunkSolvers.Select(x => x.GetReport())
                                                 .OfType<SolverReport>()
                                                 .ToArray();
        if (allReports.Length == 0)
        {
            return null;
        }

        if (_solverIdToReportData == null)
        {
            _solverIdToReportData = [];
        }

        foreach (var report in allReports)
        {
            if (!_solverIdToReportData.TryGetValue(report.ReportId.SolverId, out var previousReportData))
            {
                _solverIdToReportData.Add(report.ReportId.SolverId, (report.ReportId.ReportIteration, report, _wholeProblem.EvaluateEquation(report.BestEquation, _equationValues)));
            }

            if (report.ReportId.ReportIteration == previousReportData.ReportIteration)
            {
                continue;
            }

            _solverIdToReportData[report.ReportId.SolverId] = (report.ReportId.ReportIteration, report, _wholeProblem.EvaluateEquation(report.BestEquation, _equationValues));
        }

        (SolverReport bestChunkReport, SlimEquationScore bestChunkScore) = _solverIdToReportData.Select(x => (x.Value.Report, x.Value.SlimScore))
                                                                                                .MinBy(x => x.Item2.WrongBits);

        SolverReport bestReport = new SolverReport(allReports.Sum(x => x.IterationCount),
                                                   _fullScorer.ToFullScore(bestChunkScore, _equationValues, bestChunkReport.BestEquation),
                                                   bestChunkReport.BestEquation);

        _bestScore = bestReport.BestScore;
        return bestReport;
    }

    private sealed record ChunkExecutionData(IChunkSolver ChunkSolver,
                                             EquationProblem EquationProblem,
                                             Random Random,
                                             HashSet<int> IndexesToInsertEquationsInto,
                                             int OwnIndex);

    private readonly record struct RandomlyOrderedEquation(ProblemEquation ProblemEquation, int Priority) : IComparable<RandomlyOrderedEquation>
    {
        public int CompareTo(RandomlyOrderedEquation other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }

    public async Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        IChunkSolver[] chunkSolvers = _chunkSolvers;
        EquationProblem[] problems = _problems;

        var chunkExecutionDatas = new ChunkExecutionData[chunkSolvers.Length];
        for (int i = 0; i < chunkSolvers.Length; i++)
        {
            chunkExecutionDatas[i] = new ChunkExecutionData(chunkSolvers[i],
                                                            problems[i],
                                                            new Random(),
                                                            new HashSet<int>(),
                                                            i);
        }

        using var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);


        var parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2,
            CancellationToken = cancelSource.Token
        };

        await Parallel.ForEachAsync(chunkExecutionDatas, parallelOptions, async (chunkExecutionData, cancellationToken) =>
        {
            await chunkExecutionData.ChunkSolver.PrepareToSolveAsync(chunkExecutionData.EquationProblem, cancellationToken);
        });

        _bestScore = EquationScore.MaxScore;
        // Priority is used to randomize the order equations are taken from the channel.
        // This should ensure that it is fairly random which chunk an equation goes to.
        Channel<RandomlyOrderedEquation> sharedEquations = Channel.CreateUnboundedPrioritized<RandomlyOrderedEquation>();

        bool isRunning = true;
        int equationsToShareEachIteration = (int)(_chanceToMix * chunkExecutionDatas[0].ChunkSolver.GetEquations().Length);
        var bufferBlock = new BufferBlock<ChunkExecutionData>(new DataflowBlockOptions() { EnsureOrdered = false, CancellationToken = cancelSource.Token });
        var parallelExecutor = new ActionBlock<ChunkExecutionData>(async chunkExecutionData =>
        {
            try
            {
                if (_bestScore?.WrongBits == 0 ||
                    cancelSource.Token.IsCancellationRequested)
                {
                    isRunning = false;
                    bufferBlock.Complete();
                    return;
                }

                EquationWithScore[] chunkEquations = chunkExecutionData.ChunkSolver.GetEquations();
                int equationsToInsert = chunkExecutionData.IndexesToInsertEquationsInto.Count;
                foreach (var equationIndex in chunkExecutionData.IndexesToInsertEquationsInto)
                {
                    chunkEquations[equationIndex].Equation = (await sharedEquations.Reader.ReadAsync(cancelSource.Token)).ProblemEquation;
                    chunkEquations[equationIndex].Score = null;
                }
                chunkExecutionData.IndexesToInsertEquationsInto.Clear();

                for (int i = 0; i < _chunkSolverIterationsPerMix; i++)
                {
                    await chunkExecutionData.ChunkSolver.SolveStepAsync(chunkExecutionData.EquationProblem, cancelSource.Token);
                }

                for (int i = 0; i < equationsToShareEachIteration; i++)
                {
                    // Ensure the same index is not shared multiple times
                    int randomEquationIndex;
                    while (true)
                    {
                        randomEquationIndex = chunkExecutionData.Random.Next(0, chunkEquations.Length);
                        if (chunkExecutionData.IndexesToInsertEquationsInto.Add(randomEquationIndex))
                        {
                            break;
                        }
                    }
                    ProblemEquation equationToShare = chunkEquations[randomEquationIndex].Equation;
                    // If sharing is not working correctly then this will make it fail faster
                    chunkEquations[randomEquationIndex].Equation = null!;
                    int priority = chunkExecutionData.Random.Next(0, 10000);
                    await sharedEquations.Writer.WriteAsync(new RandomlyOrderedEquation(equationToShare, priority), cancelSource.Token);
                }

                if (!await bufferBlock.SendAsync(chunkExecutionData, cancelSource.Token) &&
                    isRunning)
                {
                    Console.WriteLine("Failed to send chunk execution data to buffer block for unknown reason.");
                    await cancelSource.CancelAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await cancelSource.CancelAsync();
                throw;
            }

        }, new ExecutionDataflowBlockOptions()
        {
            EnsureOrdered = false,
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2,
            CancellationToken = cancelSource.Token
        });

        using var link = bufferBlock.LinkTo(parallelExecutor, new DataflowLinkOptions() { PropagateCompletion = true });

        foreach (var chunkExecutionData in chunkExecutionDatas)
        {
            if (!await bufferBlock.SendAsync(chunkExecutionData, cancelSource.Token))
            {
                Console.WriteLine("Failed to send chunk execution data to buffer block.");
                await cancelSource.CancelAsync();
            }
        }

        await parallelExecutor.Completion;
    }

    public ISolver Copy()
    {
        throw new NotImplementedException();
    }
}
