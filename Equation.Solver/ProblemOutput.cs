using System.Numerics;

namespace Equation.Solver;

internal readonly record struct ProblemOutput(int[] Outputs)
{
    public int CalculateDifference(ReadOnlySpan<int> compareTo, int exampleCount)
    {
        if (compareTo.Length != Outputs.Length)
        {
            throw new ArgumentException($"Must be the same length as {Outputs}", nameof(compareTo));
        }

        int examplesMask = GetExamplesUsedMask(exampleCount);
        int difference = 0;
        for (int i = 0; i < Outputs.Length; i++)
        {
            int expected = Outputs[i] & examplesMask;
            int actual = compareTo[i] & examplesMask;
            difference += BitOperations.PopCount((uint)(expected ^ actual));
        }

        return difference;
    }

    private static int GetExamplesUsedMask(int exampleCount)
    {
        const int intBitCount = 32;
        if (exampleCount > intBitCount || exampleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exampleCount));
        }

        if (exampleCount == intBitCount)
        {
            return -1;
        }

        return (1 << exampleCount) - 1;
    }
}
