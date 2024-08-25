using Equation.Solver.Score;

namespace Equation.Solver.Solvers;

internal struct ScoredProblemEquation
{
    public SlimEquationScore Score;
    public ProblemEquation Equation;

    public ScoredProblemEquation(SlimEquationScore score, ProblemEquation equation)
    {
        Score = score;
        Equation = equation;
    }
}
