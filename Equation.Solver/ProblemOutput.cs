using System.Numerics;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly record struct ProblemOutput(Vector256<int>[] Outputs, Vector256<int> MaskBitsUsed)
{
    public int Count => Outputs.Length;

    public void CalculateDifference(ReadOnlySpan<Vector256<int>> compareTo, Span<int> bitErrors)
    {
        if (compareTo.Length != Outputs.Length)
        {
            throw new ArgumentException($"Must be the same length as {nameof(Outputs)}", nameof(compareTo));
        }
        if (bitErrors.Length != Outputs.Length)
        {
            throw new ArgumentException($"Must be the same length as {nameof(Outputs)}", nameof(bitErrors));
        }

        for (int i = 0; i < Outputs.Length; i++)
        {
            Vector256<int> expected = Outputs[i];
            Vector256<int> actual = compareTo[i] & MaskBitsUsed;
            Vector256<ulong> diff = (expected ^ actual).AsUInt64();
            bitErrors[i] += BitOperations.PopCount(diff.GetElement(0)) +
                            BitOperations.PopCount(diff.GetElement(1)) +
                            BitOperations.PopCount(diff.GetElement(2)) +
                            BitOperations.PopCount(diff.GetElement(3));
        }
    }
}
