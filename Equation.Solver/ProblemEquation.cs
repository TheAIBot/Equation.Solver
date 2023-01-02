using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class ProblemEquation
{
    private const int _unrollFactor = 4;
    private readonly NandOperator[] _nandOperators;
    private readonly int _outputSize;
    public Span<NandOperator> NandOperators => _nandOperators;
    public int OutputSize => _outputSize;

    public ProblemEquation(int operatorCount, int outputSize)
    {
        if (operatorCount % _unrollFactor != 0)
        {
            throw new ArgumentException($"Must be divisible by {_unrollFactor}.", nameof(operatorCount));
        }

        _nandOperators = new NandOperator[operatorCount];
        _outputSize = outputSize;
    }

    public unsafe ReadOnlySpan<Vector256<int>> Calculate(EquationValues equationValues)
    {
        Vector256<int>* results = equationValues.OperatorResults;
        int* allValues = (int*)equationValues.AllValues;

        // 4x loop unrolled, no bounds checks,
        // version of the code found in the else case
        fixed (NandOperator* operators = _nandOperators)
        {
            uint* indexes = ConvertNandArrayToIntArray(operators);
            for (int i = 0; i < _nandOperators.Length; i += _unrollFactor)
            {
                var op1Left = Vector256.LoadAligned(allValues + (*(indexes + 0)));
                var op1Right = Vector256.LoadAligned(allValues + (*(indexes + 1)));
                var op2Left = Vector256.LoadAligned(allValues + (*(indexes + 2)));
                var op2Right = Vector256.LoadAligned(allValues + (*(indexes + 3)));
                var op3Left = Vector256.LoadAligned(allValues + (*(indexes + 4)));
                var op3Right = Vector256.LoadAligned(allValues + (*(indexes + 5)));
                var op4Left = Vector256.LoadAligned(allValues + (*(indexes + 6)));
                var op4Right = Vector256.LoadAligned(allValues + (*(indexes + 7)));
                var result1 = NandOperator.Nand(op1Left, op1Right);
                var result2 = NandOperator.Nand(op2Left, op2Right);
                var result3 = NandOperator.Nand(op3Left, op3Right);
                var result4 = NandOperator.Nand(op4Left, op4Right);
                result1.StoreAligned((int*)(results + 0));
                result2.StoreAligned((int*)(results + 1));
                result3.StoreAligned((int*)(results + 2));
                result4.StoreAligned((int*)(results + 3));
                indexes += _unrollFactor * 2;
                results += _unrollFactor;
            }
        }

        return new Span<Vector256<int>>(((Vector256<int>*)allValues) + (equationValues._size - _outputSize), _outputSize);
    }

    private static unsafe uint* ConvertNandArrayToIntArray(NandOperator* nandArray)
    {
        // The algorithm works on the assumptions that NandOperator
        // consists of two ints. Verify this is true by checking the
        // size of a NandOperator is the same size as two ints.
        if (sizeof(NandOperator) != sizeof(int) * 2)
        {
            throw new InvalidOperationException("");
        }

        return (uint*)nandArray;
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
