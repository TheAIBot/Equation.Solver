using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

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

        var vectorIndexes = new Dictionary<Vector256<int>, int>()
        {
            // First vector should always be zeroes since that index is used for padding
            { Vector256<int>.Zero, 0 }
        };
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
            for (int i = 0; i < inputIndexes.Length; i++)
            {
                inputIndexes[i] *= Vector256<int>.Count;
            }

            var outputIndexes = new int[exampleVectors.output.outputs.Length];
            for (int i = 0; i < exampleVectors.output.outputs.Length; i++)
            {
                Vector256<int> vector = exampleVectors.output.outputs[i];
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
                outputIndexes[i] = index;
            }

            var problemInput = new ProblemInput(inputIndexes);
            var problemOutput = new ProblemOutput(outputIndexes, exampleVectors.output.mask);
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

    public static async Task<ProblemCollection> ConvertToExamples(IAsyncEnumerable<ExampleGenerator> examples, int examplePrefixCount)
    {
        ExampleGenerator[] allExamples = await examples.OrderByDescending(x => x.MaxInputLength).ToArrayAsync();

        int maxInputLength = -1;
        for (int i = 0; i < allExamples.Length; i++)
        {
            maxInputLength = Math.Max(maxInputLength, allExamples[i].MaxInputLength);
        }

        Dictionary<Rune, int> outputRuneToIndex = [];
        foreach (var outputRune in allExamples.SelectMany(x => x.UsedOutputs()))
        {
            if (outputRuneToIndex.ContainsKey(outputRune))
            {
                continue;
            }

            outputRuneToIndex.Add(outputRune, outputRuneToIndex.Count);
        }
        int maxOutputLength = outputRuneToIndex.Count;

        Dictionary<Rune, bool[]> outputRuneToOutputBools = [];
        foreach (var item in outputRuneToIndex)
        {
            bool[] outputBools = new bool[maxOutputLength];
            outputBools[item.Value] = true;
            outputRuneToOutputBools.Add(item.Key, outputBools);
        }


        var vectorIndexes = new Dictionary<Vector256<int>, int>()
        {
            // First vector should always be zeroes since that index is used for padding
            { Vector256<int>.Zero, 0 }
        };
        var problemExamples = new List<ProblemExample>();

        int totalVectors = 0;
        int deduplicatedVectors = 0;

        const int intBitCount = 32;
        int bitsPerVector = Vector256<int>.Count * intBitCount;
        foreach (ExampleGenerator[] exampleChunk in allExamples.Chunk(bitsPerVector))
        {
            // If all examples only have 3 unique examples but they all have 5
            // then there is no need to make more than 3 since the last two
            // will just be duplicated of the last unique one.
            int maxUniqueExampleCount = exampleChunk.Max(x => x.UniqueExampleCount);
            int exampleCountToMake = Math.Min(maxUniqueExampleCount, examplePrefixCount);

            bool[][] commonInputPrefixes = exampleChunk.Select(x => x.GetSharedInputPrefix()).ToArray();
            int minCommonInputPrefixLength = commonInputPrefixes.Min(x => x.Length);
            int[] sharedInputIndexes = ConvertExamplesToDeduplicatedVectors(minCommonInputPrefixLength, vectorIndexes, ref totalVectors, ref deduplicatedVectors, commonInputPrefixes);
            deduplicatedVectors += minCommonInputPrefixLength * (exampleCountToMake - 1);

            for (int examplePrefixIndex = 0; examplePrefixIndex < exampleCountToMake; examplePrefixIndex++)
            {
                bool[][] uniqueInputPostfixes = exampleChunk.Select((x, i) => commonInputPrefixes[i].Skip(minCommonInputPrefixLength).Concat(x.GetUniqueInputPostfix(examplePrefixIndex)).ToArray()).ToArray();
                int[] uniqueInputIndexes = ConvertExamplesToDeduplicatedVectors(uniqueInputPostfixes.Max(x => x.Length), vectorIndexes, ref totalVectors, ref deduplicatedVectors, uniqueInputPostfixes);
                int[] inputIndexes = new int[maxInputLength];
                sharedInputIndexes.CopyTo(inputIndexes, 0);
                uniqueInputIndexes.CopyTo(inputIndexes, sharedInputIndexes.Length);
                for (int i = 0; i < inputIndexes.Length; i++)
                {
                    inputIndexes[i] *= Vector256<int>.Count;
                }


                int[] outputIndexes = ConvertExamplesToDeduplicatedVectors(maxOutputLength, vectorIndexes, ref totalVectors, ref deduplicatedVectors, exampleChunk.Select(x => outputRuneToOutputBools[x.GetOutputRune(examplePrefixIndex)]).ToArray());

                var problemInput = new ProblemInput(inputIndexes);
                var problemOutput = new ProblemOutput(outputIndexes, CreateMask(exampleChunk.Length));
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

    private static int[] ConvertExamplesToDeduplicatedVectors(int maxInputLength, Dictionary<Vector256<int>, int> vectorIndexes, ref int totalVectors, ref int deduplicatedVectors, bool[][] problems)
    {
        var indexes = new int[maxInputLength];
        int indexVectorIndex = 0;
        foreach (Vector256<int> inputVector in ConvertToExampleVectorsNoMask(problems, maxInputLength))
        {
            ref int vectorIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(vectorIndexes, inputVector, out bool existed);
            if (!existed)
            {
                vectorIndex = vectorIndexes.Count - 1;
                totalVectors++;
            }
            else
            {
                deduplicatedVectors++;
            }
            indexes[indexVectorIndex] = vectorIndex;
            indexVectorIndex++;
        }

        return indexes;
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

    private static IEnumerable<Vector256<int>> ConvertToExampleVectorsNoMask(bool[][] exampleChunk, int maxExampleLength)
    {
        const int intBitCount = 32;
        var exampleInt32x8 = new int[Vector256<int>.Count];

        for (int i = 0; i < maxExampleLength; i++)
        {
            int exampleIndex = 0;
            for (int elementIndex = 0; elementIndex < Vector256<int>.Count; elementIndex++)
            {
                int exampleElement = 0;
                int remainingBits = Math.Min(intBitCount, exampleChunk.Length - exampleIndex);
                for (int bitIndex = 0; bitIndex < remainingBits; bitIndex++)
                {
                    bool[] example = exampleChunk[exampleIndex];
                    exampleIndex++;
                    if (i < example.Length)
                    {
                        exampleElement |= (example[i] ? 1 : 0) << bitIndex;
                    }
                }

                exampleInt32x8[elementIndex] = exampleElement;
            }

            yield return Vector256.Create(exampleInt32x8);
        }
    }

    private static Vector256<int> CreateMask(int bitsUsed)
    {
        const int intBitCount = 32;
        Span<int> exampleMask32x8 = stackalloc int[Vector256<int>.Count];
        for (int i = 0; i < bitsUsed; i++)
        {
            exampleMask32x8[i / intBitCount] |= 1 << (i % intBitCount);
        }

        return Vector256.Create(exampleMask32x8);
    }
}
