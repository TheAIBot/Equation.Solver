namespace Equation.Solver;

internal sealed record SolverReport(ReportId ReportId, long IterationCount, EquationScore BestScore, ProblemEquation BestEquation)
{
    public SolverReport(long iterationCount, EquationScore bestScore, ProblemEquation bestEquation) : this(default, iterationCount, bestScore, bestEquation) { }
}