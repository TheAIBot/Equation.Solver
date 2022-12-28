namespace Equation.Solver.Solvers;

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
