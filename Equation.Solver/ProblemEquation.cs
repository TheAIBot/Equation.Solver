using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Equation.Solver;

internal sealed class ProblemEquation
{
    private const int _unrollFactor = 4;
    private readonly NandOperator[] _nandOperators;
    private readonly int _outputSize;
    public Span<NandOperator> NandOperators => _nandOperators;

    public ProblemEquation(int operatorCount, int outputSize)
    {
        if (operatorCount % _unrollFactor != 0)
        {
            throw new ArgumentException($"Must be divisible by {_unrollFactor}.", nameof(operatorCount));
        }

        _nandOperators = new NandOperator[operatorCount];
        _outputSize = outputSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<Vector256<int>> Calculate(EquationValues equationValues)
    {
        Vector256<int>* results = equationValues.OperatorResults;
        Vector256<int>* allValues = equationValues.AllValues;
        if (Avx2.IsSupported)
        {
            // Avx2 optimized, 4x loop unrolled, no bounds checks,
            // version of the code found in the else case
            fixed (NandOperator* operators = _nandOperators)
            {
                int* indexes = ConvertNandArrayToIntArray(operators);
                for (int i = 0; i < _nandOperators.Length; i += _unrollFactor)
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
                    indexes += _unrollFactor * 2;
                    results += _unrollFactor;
                }
            }
        }
        else
        {
            for (int i = 0; i < _nandOperators.Length; i++)
            {
                var leftValue = allValues[_nandOperators[i]._leftValueIndex];
                var rightValue = allValues[_nandOperators[i]._rightValueIndex];
                results[i] = ~(leftValue & rightValue);
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
