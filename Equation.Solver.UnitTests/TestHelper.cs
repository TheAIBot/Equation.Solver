using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Equation.Solver.UnitTests;

internal static class TestHelper
{
    public static unsafe Span<Vector256<int>> CreateAlignedSpan(int count)
    {
        return new Span<Vector256<int>>(NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<int>) * count), (nuint)sizeof(Vector256<int>)), count);
    }
}