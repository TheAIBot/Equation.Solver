using System.Numerics;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly record struct ProblemOutput(int[] Indexes, Vector256<int> MaskBitsUsed)
{
    public int Count => Indexes.Length;

    public unsafe void CalculateDifference(ReadOnlySpan<Vector256<int>> compareTo, Span<int> bitErrors, ProblemCollection problemCollection)
    {
        if (compareTo.Length != Indexes.Length)
        {
            throw new ArgumentException($"Must be the same length as {nameof(Indexes)}", nameof(compareTo));
        }
        if (bitErrors.Length != Indexes.Length)
        {
            throw new ArgumentException($"Must be the same length as {nameof(Indexes)}", nameof(bitErrors));
        }

        int* vectors = (int*)problemCollection.Vectors;
        int[] indexes = Indexes;

        for (int i = 0; i < indexes.Length; i++)
        {
            Vector256<int> expected = Vector256.LoadAligned(vectors + indexes[i] * Vector256<int>.Count);
            Vector256<int> actual = compareTo[i] & MaskBitsUsed;
            Vector256<ulong> diff = (expected ^ actual).AsUInt64();
            bitErrors[i] += BitOperations.PopCount(diff.GetElement(0)) +
                            BitOperations.PopCount(diff.GetElement(1)) +
                            BitOperations.PopCount(diff.GetElement(2)) +
                            BitOperations.PopCount(diff.GetElement(3));
        }
    }

    public unsafe bool[] GetExampleCorrectness(ReadOnlySpan<Vector256<int>> compareTo, ProblemCollection problemCollection)
    {
        if (compareTo.Length != Indexes.Length)
        {
            throw new ArgumentException($"Must be the same length as {nameof(Indexes)}", nameof(compareTo));
        }

        int* vectors = (int*)problemCollection.Vectors;
        int[] indexes = Indexes;

        var combinedDiff = Vector256<int>.Zero;
        for (int i = 0; i < indexes.Length; i++)
        {
            Vector256<int> expected = Vector256.LoadAligned(vectors + indexes[i] * Vector256<int>.Count);
            Vector256<int> actual = compareTo[i] & MaskBitsUsed;
            combinedDiff |= expected ^ actual;
        }

        int exampleCount = 0;
        for (int i = 0; i < Vector256<int>.Count; i++)
        {
            exampleCount += BitOperations.PopCount((uint)MaskBitsUsed.GetElement(i));
        }

        var results = new bool[exampleCount];
        const int bitsPerInt = 32;
        for (int elementIndex = 0; elementIndex < exampleCount; elementIndex += bitsPerInt)
        {
            uint diff = (uint)combinedDiff.GetElement(elementIndex / bitsPerInt);

            int exampleCountInElement = Math.Min(exampleCount - elementIndex, bitsPerInt);
            for (int i = 0; i < exampleCountInElement; i++)
            {
                results[elementIndex + i] = ((diff >> i) & 1) == 0;
            }
        }

        return results;
    }
}
