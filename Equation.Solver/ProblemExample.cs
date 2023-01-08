using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly record struct ProblemExample(ProblemInput Input, ProblemOutput Output)
{
    public static IEnumerable<ProblemExample> ConvertToExamples(IEnumerable<(bool[] inputs, bool[] outputs)> examples)
    {
        var inputs = ConvertToExampleVectors(examples.Select(x => x.inputs));
        var outputs = ConvertToExampleVectors(examples.Select(x => x.outputs));
        foreach (((Vector256<int>[] inputs, Vector256<int> mask) input, (Vector256<int>[] outputs, Vector256<int> mask) output) exampleVectors in inputs.Zip(outputs))
        {
            var problemInput = new ProblemInput(exampleVectors.input.inputs);
            var problemOutput = new ProblemOutput(exampleVectors.output.outputs, exampleVectors.output.mask);
            yield return new ProblemExample(problemInput, problemOutput);
        }
    }

    private static IEnumerable<(Vector256<int>[] values, Vector256<int> mask)> ConvertToExampleVectors(IEnumerable<bool[]> examples)
    {
        return ConvertToExampleVectors(ConvertToExampleInts(examples));
    }

    private static IEnumerable<(Vector256<int>[] values, Vector256<int> mask)> ConvertToExampleVectors(IEnumerable<(int[] values, int mask)> examplesAsInts)
    {
        int? bitLength = null;
        foreach ((int[][] exampleChunk, int[] masks) in examplesAsInts.Chunk(Vector256<int>.Count).Select(x => (x.Select(y => y.values).ToArray(), x.Select(y => y.mask).ToArray())))
        {
            if (!bitLength.HasValue)
            {
                bitLength = exampleChunk[0].Length;
            }
            AssertAllArraysAreSameLength(exampleChunk, bitLength.Value);

            var exampleVectors = new List<Vector256<int>>();
            var exampleInt32x8 = new int[Vector256<int>.Count];
            for (int i = 0; i < exampleChunk[0].Length; i++)
            {
                Array.Clear(exampleInt32x8);
                for (int x = 0; x < exampleChunk.Length; x++)
                {
                    exampleInt32x8[x] = exampleChunk[x][i];
                }

                exampleVectors.Add(Vector256.Create(exampleInt32x8));
            }

            Array.Clear(exampleInt32x8);
            for (int x = 0; x < exampleChunk.Length; x++)
            {
                exampleInt32x8[x] = masks[x];
            }
            var mask = Vector256.Create(exampleInt32x8);

            yield return (exampleVectors.ToArray(), mask);
        }
    }

    private static IEnumerable<(int[] values, int mask)> ConvertToExampleInts(IEnumerable<bool[]> examples)
    {
        int? bitLength = null;
        const int intBitCount = 32;
        foreach (bool[][] boolExamples in examples.Chunk(intBitCount))
        {
            if (!bitLength.HasValue)
            {
                bitLength = boolExamples[0].Length;
            }
            AssertAllArraysAreSameLength(boolExamples, bitLength.Value);


            int[] exampleInts = new int[boolExamples[0].Length];
            int mask = 0;
            for (int i = 0; i < boolExamples.Length; i++)
            {
                for (int x = 0; x < boolExamples[0].Length; x++)
                {
                    exampleInts[x] |= (boolExamples[i][x] ? 1 : 0) << i;
                }
                mask |= 1 << i;
            }

            yield return (exampleInts, mask);
        }
    }

    private static void AssertAllArraysAreSameLength<T>(T[][] arrays, int expectedLength)
    {
        for (int i = 0; i < arrays.Length; i++)
        {
            if (arrays[i].Length != expectedLength)
            {
                throw new InvalidOperationException("Not all arrays has the same length.");
            }
        }
    }
}
