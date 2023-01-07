using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class ProblemEquation
{
    private readonly NandOperator[] _nandOperators;
    private readonly int _outputSize;
    public Span<NandOperator> NandOperators => _nandOperators;
    public int OutputSize => _outputSize;

    public ProblemEquation(int operatorCount, int outputSize)
    {
        _nandOperators = new NandOperator[operatorCount];
        _outputSize = outputSize;
    }

    public unsafe ReadOnlySpan<Vector256<int>> Calculate(EquationValues equationValues)
    {
        Vector256<int>* results = equationValues.OperatorResults;
        int* allValues = (int*)equationValues.AllValues;

        fixed (NandOperator* operators = _nandOperators)
        {
            for (int i = 0; i < _nandOperators.Length; i++)
            {
                var result = _nandOperators[i].Nand(allValues);
                result.StoreAligned((int*)(results + i));
            }
        }

        return new Span<Vector256<int>>(((Vector256<int>*)allValues) + (equationValues._size - _outputSize), _outputSize);
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
