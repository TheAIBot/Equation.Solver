using System.Numerics;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly record struct ProblemOutput(Vector256<int>[] Outputs)
{
    public int Count => Outputs.Length;

    public int CalculateDifference(ReadOnlySpan<Vector256<int>> compareTo)
    {
        if (compareTo.Length != Outputs.Length)
        {
            throw new ArgumentException($"Must be the same length as {Outputs}", nameof(compareTo));
        }

        int difference = 0;
        for (int i = 0; i < Outputs.Length; i++)
        {
            Vector256<int> expected = Outputs[i];
            Vector256<int> actual = compareTo[i];
            Vector256<ulong> diff = (expected ^ actual).AsUInt64();
            difference += BitOperations.PopCount(diff.GetElement(0)) +
                          BitOperations.PopCount(diff.GetElement(1)) +
                          BitOperations.PopCount(diff.GetElement(2)) +
                          BitOperations.PopCount(diff.GetElement(3));
        }

        return difference;
    }
}
