using System.Runtime.Intrinsics;
using static Equation.Solver.Program;

namespace Equation.Solver;

internal interface IExampleCluster
{
    List<T>[] ToClusters<T>(IEnumerable<T> examples);
}

internal sealed class RandomExampleCluster : IExampleCluster
{
    private readonly RandomExampleClustering[] _groupings;

    public RandomExampleCluster(RandomExampleClustering[] randomGroupings)
    {
        _groupings = randomGroupings;
    }

    public List<T>[] ToClusters<T>(IEnumerable<T> examples)
    {
        int totalClusterCount = _groupings.Sum(x => x.ClusterssWithThisChance);
        var exampleClusters = new List<T>[totalClusterCount];

        for (int i = 0; i < exampleClusters.Length; i++)
        {
            exampleClusters[i] = [];
        }

        var random = new Random(64789);
        foreach (var example in examples)
        {
            int index = 0;
            foreach (var exampleGrouping in _groupings)
            {
                for (int groupIndex = 0; groupIndex < exampleGrouping.ClusterssWithThisChance; groupIndex++)
                {
                    if (random.NextSingle() <= exampleGrouping.PercentOfAllProblems)
                    {
                        exampleClusters[index].Add(example);
                    }

                    index++;
                }
            }
        }

        return exampleClusters;
    }
}

internal readonly record struct RandomExampleClustering(float PercentOfAllProblems, int ClusterssWithThisChance);

internal readonly record struct ProblemExample(ProblemInput Input, ProblemOutput Output)
{
    public static ProblemCollection ConvertToExamples((bool[] inputs, bool[] outputs)[] examples)
    {
        int maxInputLength = -1;
        int maxOutputLength = -1;
        for (int i = 0; i < examples.Length; i++)
        {
            maxInputLength = Math.Max(maxInputLength, examples[i].inputs.Length);
            maxOutputLength = Math.Max(maxOutputLength, examples[i].outputs.Length);
        }
        var inputs = ConvertToExampleVectors(examples.Select(x => x.inputs), maxInputLength);
        var outputs = ConvertToExampleVectors(examples.Select(x => x.outputs), maxOutputLength);

        var vectorIndexes = new Dictionary<Vector256<int>, int>();
        var problemExamples = new List<ProblemExample>();

        int totalVectors = 0;
        int deduplicatedVectors = 0;

        foreach (((Vector256<int>[] inputs, Vector256<int> mask) input, (Vector256<int>[] outputs, Vector256<int> mask) output) exampleVectors in inputs.Zip(outputs))
        {
            var inputIndexes = new int[exampleVectors.input.inputs.Length];
            for (int i = 0; i < exampleVectors.input.inputs.Length; i++)
            {
                Vector256<int> vector = exampleVectors.input.inputs[i];
                if (!vectorIndexes.TryGetValue(vector, out int index))
                {
                    index = vectorIndexes.Count;
                    vectorIndexes[vector] = index;
                    totalVectors++;
                }
                else
                {
                    deduplicatedVectors++;
                }
                inputIndexes[i] = index;
            }

            var problemInput = new ProblemInput(inputIndexes);
            var problemOutput = new ProblemOutput(exampleVectors.output.outputs, exampleVectors.output.mask);
            problemExamples.Add(new ProblemExample(problemInput, problemOutput));
        }

        Console.WriteLine($"Total vectors: {totalVectors:N0}");
        Console.WriteLine($"Deduplicated vectors: {deduplicatedVectors:N0}");

        var uniqueVectors = new Vector256<int>[vectorIndexes.Count];
        foreach (var pair in vectorIndexes)
        {
            uniqueVectors[pair.Value] = pair.Key;
        }

        return ProblemCollection.Create(problemExamples.ToArray(), uniqueVectors);
    }

    public static ProblemCollection ConvertToExamples(ExampleGenerator[] examples, int examplePrefixCount)
    {
        int maxInputLength = -1;
        int maxOutputLength = -1;
        for (int i = 0; i < examples.Length; i++)
        {
            maxInputLength = Math.Max(maxInputLength, examples[i].MaxInputLength);
            maxOutputLength = Math.Max(maxOutputLength, examples[i].MaxOutputLength);
        }

        Array.Sort(examples, (x, y) => x.MaxInputLength - y.MaxInputLength);


        var vectorIndexes = new Dictionary<Vector256<int>, int>();
        var problemExamples = new List<ProblemExample>();

        int totalVectors = 0;
        int deduplicatedVectors = 0;

        const int intBitCount = 32;
        int bitsPerVector = Vector256<int>.Count * intBitCount;
        foreach (ExampleGenerator[] exampleChunk in examples.Chunk(bitsPerVector))
        {
            for (int examplePrefixIndex = 0; examplePrefixIndex < examplePrefixCount; examplePrefixIndex++)
            {
                (Vector256<int>[] inputValues, Vector256<int> _) = ConvertToExampleVectors(exampleChunk.Select(x => x.GetInput(examplePrefixIndex)), maxInputLength).Single();
                (Vector256<int>[] outputValues, Vector256<int> outputMask) = ConvertToExampleVectors(exampleChunk.Select(x => x.GetOutput(examplePrefixIndex)), maxOutputLength).Single();

                var inputIndexes = new int[maxInputLength];
                for (int inputVectorIndex = 0; inputVectorIndex < maxInputLength; inputVectorIndex++)
                {
                    Vector256<int> vector = inputValues[inputVectorIndex];
                    if (!vectorIndexes.TryGetValue(vector, out int index))
                    {
                        index = vectorIndexes.Count;
                        vectorIndexes[vector] = index;
                        totalVectors++;
                    }
                    else
                    {
                        deduplicatedVectors++;
                    }
                    inputIndexes[inputVectorIndex] = index;
                }

                var problemInput = new ProblemInput(inputIndexes);
                var problemOutput = new ProblemOutput(outputValues, outputMask);
                problemExamples.Add(new ProblemExample(problemInput, problemOutput));
            }
        }

        Console.WriteLine($"Total vectors: {totalVectors:N0}");
        Console.WriteLine($"Deduplicated vectors: {deduplicatedVectors:N0}");

        var uniqueVectors = new Vector256<int>[vectorIndexes.Count];
        foreach (var pair in vectorIndexes)
        {
            uniqueVectors[pair.Value] = pair.Key;
        }

        return ProblemCollection.Create(problemExamples.ToArray(), uniqueVectors);
    }

    private static IEnumerable<(Vector256<int>[] values, Vector256<int> mask)> ConvertToExampleVectors(IEnumerable<bool[]> examples, int maxExampleLength)
    {
        const int intBitCount = 32;
        var exampleInt32x8 = new int[Vector256<int>.Count];
        var exampleMask32x8 = new int[Vector256<int>.Count];

        foreach (bool[][] exampleChunk in examples.Chunk(Vector256<int>.Count * intBitCount))
        {
            Array.Clear(exampleMask32x8);

            var exampleVectors = new Vector256<int>[maxExampleLength];
            for (int i = 0; i < maxExampleLength; i++)
            {
                Array.Clear(exampleInt32x8);
                for (int x = 0; x < exampleChunk.Length; x++)
                {
                    bool[] example = exampleChunk[x];
                    if (i < example.Length)
                    {
                        exampleInt32x8[x / intBitCount] |= (example[i] ? 1 : 0) << (x % intBitCount);

                        if (i == 0)
                        {
                            exampleMask32x8[x / intBitCount] |= 1 << (x % intBitCount);
                        }
                    }
                }

                exampleVectors[i] = Vector256.Create(exampleInt32x8);
            }

            var mask = Vector256.Create(exampleMask32x8);
            yield return (exampleVectors, mask);
        }
    }
}
