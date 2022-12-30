using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Equation.Solver;

internal readonly struct NandOperator
{
    public readonly int _leftValueIndex;
    public readonly int _rightValueIndex;

    public NandOperator(int leftValueIndex, int rightValueIndex)
    {
        // Pre-multiplied by 8 since these are not an index into
        // a Vector256<int>[] but an int[]  where 8 elements
        // together are an Vector256<int>.
        _leftValueIndex = leftValueIndex * Vector256<int>.Count;
        _rightValueIndex = rightValueIndex * Vector256<int>.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> Nand(Vector256<int> left, Vector256<int> right)
    {
        // C# does not yet optimize ~(x & y) into Avx2.AndNot(x, y)
        // so here it's done manually instead
        if (Avx2.IsSupported)
        {
            return Avx2.AndNot(left, right);
        }
        else
        {
            return ~(left & right);
        }
    }
}