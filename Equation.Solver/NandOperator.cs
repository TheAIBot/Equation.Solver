using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly struct NandOperator
{
    private readonly uint _leftValueIndex;
    private readonly uint _rightValueIndex;

    public readonly int LeftValueIndex => (int)(_leftValueIndex / (uint)Vector256<int>.Count);
    public readonly int RightValueIndex => (int)(_rightValueIndex / (uint)Vector256<int>.Count);

    public NandOperator(int leftValueIndex, int rightValueIndex)
    {
        // Pre-multiplied by 8 since these are not an index into
        // a Vector256<int>[] but an int[]  where 8 elements
        // together are an Vector256<int>.
        _leftValueIndex = (uint)(leftValueIndex * Vector256<int>.Count);
        _rightValueIndex = (uint)(rightValueIndex * Vector256<int>.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe Vector256<int> Nand(int* allValues, int* inputIndexes, int* vectors, int inputCount)
    {
        int* leftValue;
        if (_leftValueIndex < inputCount)
        {
            int vectorIndex = inputIndexes[_leftValueIndex / (uint)Vector256<int>.Count];
            leftValue = vectors + vectorIndex * Vector256<int>.Count;
        }
        else
        {
            leftValue = allValues + _leftValueIndex;
        }

        int* rightValue;
        if (_rightValueIndex < inputCount)
        {
            int vectorIndex = inputIndexes[_rightValueIndex / (uint)Vector256<int>.Count];
            rightValue = vectors + vectorIndex * Vector256<int>.Count;
        }
        else
        {
            rightValue = allValues + _rightValueIndex;
        }

        Vector256<int> opLeft = Vector256.LoadAligned(leftValue);
        Vector256<int> opRight = Vector256.LoadAligned(rightValue);
        return ~(opLeft & opRight);
    }
}