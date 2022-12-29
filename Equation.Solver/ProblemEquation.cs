using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class ProblemEquation
{

    private readonly NandOperator[] _nandOperators;
    private readonly int _outputSize;
    public Span<NandOperator> NandOperators => _nandOperators;

    public ProblemEquation(int operatorCount, int outputSize)
    {
        _nandOperators = new NandOperator[operatorCount];
        _outputSize = outputSize;
    }

    public ReadOnlySpan<Vector256<int>> Calculate(EquationValues equationValues)
    {
        Span<Vector256<int>> results = equationValues.OperatorResults.Span;
        Vector256<int>[] allValues = equationValues.AllValues;
        for (int i = 0; i < _nandOperators.Length; i++)
        {
            results[i] = _nandOperators[i].Calculate(allValues);
        }

        return results.Slice(results.Length - _outputSize, _outputSize);
    }

    public ProblemEquation Copy()
    {
        var copy = new ProblemEquation(_nandOperators.Length, _outputSize);
        copy.CopyFrom(this);

        return copy;
    }

    public void CopyFrom(ProblemEquation copyFrom)
    {
        Array.Copy(copyFrom._nandOperators, _nandOperators, copyFrom._nandOperators.Length);
    }
}
