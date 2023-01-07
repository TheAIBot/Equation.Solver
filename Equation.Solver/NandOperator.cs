using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly struct NandOperator
{
    private readonly int _leftValueIndex;
    private readonly int _rightValueIndex;

    public int LeftValueIndex => _leftValueIndex / Vector256<int>.Count;
    public int RightValueIndex => _rightValueIndex / Vector256<int>.Count;

    public NandOperator(int leftValueIndex, int rightValueIndex)
    {
        // Pre-multiplied by 8 since these are not an index into
        // a Vector256<int>[] but an int[]  where 8 elements
        // together are an Vector256<int>.
        _leftValueIndex = leftValueIndex * Vector256<int>.Count;
        _rightValueIndex = rightValueIndex * Vector256<int>.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Vector256<int> Nand(int* allValues)
    {
        var opLeft = Vector256.LoadAligned(allValues + _leftValueIndex);
        var opRight = Vector256.LoadAligned(allValues + _rightValueIndex);
        return ~(opLeft & opRight);
    }
}