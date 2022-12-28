namespace Equation.Solver.Solvers;

internal interface IMultipleReporting : IReporting
{
    SolverReport[] GetAllReports();
}
