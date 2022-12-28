namespace Equation.Solver.Solvers;

internal interface IChunkEvolver : IReporting
{
    Span<ScoredProblemEquation> Equations { get; }
    int BestScore { get; }

    void EvolveChunk(EquationProblem problem);

    void UpdateBestEquation();

    IChunkEvolver Copy();
}

internal struct ScoredProblemEquation
{
    public int Score;
    public ProblemEquation Equation;

    public ScoredProblemEquation(int score, ProblemEquation equation)
    {
        Score = score;
        Equation = equation;
    }
}
