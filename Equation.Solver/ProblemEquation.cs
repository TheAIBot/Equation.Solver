using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Equation.Solver;

internal sealed class ProblemEquation
{

    private readonly NandOperator[] _nandOperators;
    private readonly int _outputSize;
    public Span<NandOperator> NandOperators => _nandOperators;

    public ProblemEquation(int operatorCount, int outputSize)
    {
        if (operatorCount % 4 != 0)
        {
            throw new ArgumentException("Must be divisible by 4.", nameof(operatorCount));
        }
        _nandOperators = new NandOperator[operatorCount];
        _outputSize = outputSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<Vector256<int>> Calculate(EquationValues equationValues)
    {
        Vector256<int>* results = equationValues.OperatorResults;
        Vector256<int>* allValues = equationValues.AllValues;
        fixed (NandOperator* operators = _nandOperators)
        {
            int* indexes = ConvertNandArrayToIntArray(operators);
            for (int i = 0; i < _nandOperators.Length; i += 4)
            {
                var op1Left = Avx.LoadAlignedVector256((int*)(allValues + (*(indexes + 0))));
                var op1Right = Avx.LoadAlignedVector256((int*)(allValues + (*(indexes + 1))));
                var op2Left = Avx.LoadAlignedVector256((int*)(allValues + (*(indexes + 2))));
                var op2Right = Avx.LoadAlignedVector256((int*)(allValues + (*(indexes + 3))));
                var op3Left = Avx.LoadAlignedVector256((int*)(allValues + (*(indexes + 4))));
                var op3Right = Avx.LoadAlignedVector256((int*)(allValues + (*(indexes + 5))));
                var op4Left = Avx.LoadAlignedVector256((int*)(allValues + (*(indexes + 6))));
                var op4Right = Avx.LoadAlignedVector256((int*)(allValues + (*(indexes + 7))));
                var result1 = Avx2.AndNot(op1Left, op1Right);
                var result2 = Avx2.AndNot(op2Left, op2Right);
                var result3 = Avx2.AndNot(op3Left, op3Right);
                var result4 = Avx2.AndNot(op4Left, op4Right);
                Avx.StoreAligned((int*)(results + 0), result1);
                Avx.StoreAligned((int*)(results + 1), result2);
                Avx.StoreAligned((int*)(results + 2), result3);
                Avx.StoreAligned((int*)(results + 3), result4);
                indexes += 8;
                results += 4;
            }
        }

        return new Span<Vector256<int>>(allValues + (equationValues._size - _outputSize), _outputSize);
    }

    private static unsafe int* ConvertNandArrayToIntArray(NandOperator* nandArray)
    {
        // The algorithm works on the assumptions that NandOperator
        // consists of two ints. Verify this is true by checking the
        // size of a NandOperator is the same size as two ints.
        if (sizeof(NandOperator) != sizeof(int) * 2)
        {
            throw new InvalidOperationException("");
        }

        return (int*)nandArray;
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
