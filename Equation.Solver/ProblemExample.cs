using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly record struct ProblemExample(ProblemInput Input, ProblemOutput Output)
{
    public static IEnumerable<ProblemExample> ConvertToExamples(IEnumerable<(bool[] inputs, bool[] outputs)> examples)
    {
        var inputs = ConvertToExampleVectors(examples.Select(x => x.inputs));
        var outputs = ConvertToExampleVectors(examples.Select(x => x.outputs));
        foreach ((Vector256<int>[] inputs, Vector256<int>[] outputs) exampleVectors in inputs.Zip(outputs))
        {
            var problemInput = new ProblemInput(exampleVectors.inputs);
            var problemOutput = new ProblemOutput(exampleVectors.outputs);
            yield return new ProblemExample(problemInput, problemOutput);
        }
    }

    private static IEnumerable<Vector256<int>[]> ConvertToExampleVectors(IEnumerable<bool[]> examples)
    {
        return ConvertToExampleVectors(ConvertToExampleInts(examples));
    }

    private static IEnumerable<Vector256<int>[]> ConvertToExampleVectors(IEnumerable<int[]> examplesAsInts)
    {
        int? bitLength = null;
        foreach (int[][] exampleChunk in examplesAsInts.Chunk(Vector256<int>.Count))
        {
            if (!bitLength.HasValue)
            {
                bitLength = exampleChunk[0].Length;
            }
            AssertAllArraysAreSameLength(exampleChunk, bitLength.Value);

            var exampleVectors = new List<Vector256<int>>();
            for (int i = 0; i < exampleChunk[0].Length; i++)
            {
                var exampleInt32x8 = new int[Vector256<int>.Count];
                for (int x = 0; x < exampleChunk.Length; x++)
                {
                    exampleInt32x8[x] = exampleChunk[x][i];
                }

                exampleVectors.Add(Vector256.Create(exampleInt32x8));
            }

            yield return exampleVectors.ToArray();
        }
    }

    private static IEnumerable<int[]> ConvertToExampleInts(IEnumerable<bool[]> examples)
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
            for (int i = 0; i < boolExamples.Length; i++)
            {
                for (int x = 0; x < boolExamples[0].Length; x++)
                {
                    exampleInts[x] |= (boolExamples[i][x] ? 1 : 0) << i;
                }
            }

            yield return exampleInts;
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
