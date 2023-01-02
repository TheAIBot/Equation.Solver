namespace Equation.Solver;

internal sealed record SolverReport(long IterationCount, EquationScore BestScore, ProblemEquation BestEquation);
