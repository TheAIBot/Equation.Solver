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
    public readonly unsafe Vector256<int> Nand(int* allValues, int* inputs, int inputCount)
    {
        int* leftInput = _leftValueIndex < inputCount * Vector256<int>.Count ? inputs : allValues;
        Vector256<int> opLeft = Vector256.LoadAligned(leftInput + _leftValueIndex);

        int* rightInput = _rightValueIndex < inputCount * Vector256<int>.Count ? inputs : allValues;
        Vector256<int> opRight = Vector256.LoadAligned(rightInput + _rightValueIndex);

        return ~(opLeft & opRight);
    }
}