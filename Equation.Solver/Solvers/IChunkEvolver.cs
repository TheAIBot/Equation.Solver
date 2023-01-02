namespace Equation.Solver.Solvers;

internal interface IChunkEvolver : IReporting
{
    ScoredProblemEquation[] Equations { get; }
    EquationScore BestScore { get; }

    void EvolveChunk(EquationProblem problem);

    void UpdateBestEquation();

    IChunkEvolver Copy(int randomSeed);
}