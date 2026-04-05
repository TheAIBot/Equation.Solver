using Equation.Solver.Score;

namespace Equation.Solver.Solvers;

internal sealed class ParallelMixSolver : ISolver
{
    private readonly IChunkSolver[] _chunkSolvers;
    private readonly EquationProblem[] _problems;
    private readonly EquationProblem _wholeProblem;
    private readonly int _chunkSolverIterationsPerMix;
    private readonly float _chanceToMix;
    private readonly EquationValues _equationValues;
    private readonly FullScorer _fullScorer = new FullScorer();
    private EquationScore? _bestScore;

    public ParallelMixSolver(IChunkSolver chunkSolver,
                             EquationProblem[] problems,
                             EquationProblem wholeProblem,
                             int operatorCount,
                             int chunkSolverIterationsPerMix,
                             float chanceToMix)
    {
        _chunkSolvers = problems.Select(_ => chunkSolver.CopyChunkSolver())
                                        .ToArray();
        _problems = problems;
        _wholeProblem = wholeProblem;
        _chunkSolverIterationsPerMix = chunkSolverIterationsPerMix;
        _chanceToMix = chanceToMix;
        _equationValues = new EquationValues(wholeProblem.ParameterCount, operatorCount);
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

        (SolverReport report, SlimEquationScore fullScore) = allReports.Select(x => (x, _wholeProblem.EvaluateEquation(x.BestEquation, _equationValues)))
                                                                       .MinBy(x => x.Item2.WrongBits);

        SolverReport bestReport = new SolverReport(allReports.Sum(x => x.IterationCount),
                                                   _fullScorer.ToFullScore(fullScore, _equationValues, report.BestEquation),
                                                   report.BestEquation);

        _bestScore = bestReport.BestScore;
        return bestReport;
    }

    public async Task SolveAsync(EquationProblem problem, CancellationToken cancellationToken)
    {
        IChunkSolver[] chunkSolvers = _chunkSolvers;
        EquationProblem[] problems = _problems;

        var parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2,
            CancellationToken = cancellationToken
        };

        var chunkSolversWithProblems = chunkSolvers.Zip(problems).ToArray();
        await Parallel.ForEachAsync(chunkSolversWithProblems, parallelOptions, async (chunkSolverWithProblem, cancellationToken) =>
        {
            await chunkSolverWithProblem.First.PrepareToSolveAsync(chunkSolverWithProblem.Second, cancellationToken);
        });

        var random = new Random(398906);
        EquationWithScore[] randomizedEquations = new EquationWithScore[chunkSolvers.Length];
        // Assumes the array of equations never change over time
        EquationWithScore[][] chunkEquations = chunkSolvers.Select(x => x.GetEquations()).ToArray();

        _bestScore = EquationScore.MaxScore;
        while (_bestScore?.WrongBits != 0 && !cancellationToken.IsCancellationRequested)
        {
            if (_bestScore?.WrongBits == 0 ||
                cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Parallel.ForEachAsync(chunkSolversWithProblems, parallelOptions, async (chunkSolverWithProblem, cancellationToken) =>
            {
                for (int i = 0; i < _chunkSolverIterationsPerMix; i++)
                {
                    await chunkSolverWithProblem.First.SolveStepAsync(chunkSolverWithProblem.Second, cancellationToken);
                }
            });

            for (int equationIndex = 0; equationIndex < chunkEquations[0].Length; equationIndex++)
            {
                if (random.NextSingle() > _chanceToMix)
                {
                    continue;
                }

                for (int chunkIndex = 0; chunkIndex < chunkSolvers.Length; chunkIndex++)
                {
                    randomizedEquations[chunkIndex] = chunkEquations[chunkIndex][equationIndex];
                    randomizedEquations[chunkIndex].Score = null;
                }

                random.Shuffle(randomizedEquations);

                for (int chunkIndex = 0; chunkIndex < chunkSolvers.Length; chunkIndex++)
                {
                    chunkEquations[chunkIndex][equationIndex] = randomizedEquations[chunkIndex];
                }
            }
        }
    }

    public ISolver Copy()
    {
        throw new NotImplementedException();
    }
}
