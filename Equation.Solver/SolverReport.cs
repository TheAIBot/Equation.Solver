namespace Equation.Solver;

internal sealed record SolverReport(long IterationCount, int BestScore, ProblemEquation BestEquation);
