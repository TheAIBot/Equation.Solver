using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed unsafe class ProblemCollection
{
    private readonly Vector256<int>* _vectors;
    private readonly int _vectorCount;

    public ProblemExampleBatch[] Examples { get; }
    public Vector256<int>* Vectors => _vectors;
    public int VectorCount => _vectorCount;
    public int MaxBatchSize { get; }

    public ProblemCollection(ProblemExampleBatch[] examples, Vector256<int>* vectors, int vectorCount, int maxBatchSize)
    {
        Examples = examples;
        _vectors = vectors;
        _vectorCount = vectorCount;
        MaxBatchSize = maxBatchSize;
    }

    public static ProblemCollection Create(ProblemExample[] examples, Vector256<int>[] uniqueVectors, int maxBatchSize)
    {
        Vector256<int>* vectors = (Vector256<int>*)NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<int>) * uniqueVectors.Length),
                                                                             (nuint)sizeof(Vector256<int>));
        for (int i = 0; i < uniqueVectors.Length; i++)
        {
            vectors[i] = uniqueVectors[i];
        }

        return new ProblemCollection(examples.Chunk(maxBatchSize).Select(CreateBatch).ToArray(), vectors, uniqueVectors.Length, maxBatchSize);
    }

    public IEnumerable<ProblemExample> GetIndividualExamples()
    {
        for (int exampleIndex = 0; exampleIndex < Examples.Length; exampleIndex++)
        {
            ProblemExampleBatch problemExample = Examples[exampleIndex];
            for (int batchIndex = 0; batchIndex < problemExample.BatchSize; batchIndex++)
            {
                int[] inputIndexes = new int[problemExample.InputCount];
                int[] outputIndexes = new int[problemExample.OutputCount];
                for (int i = 0; i < inputIndexes.Length; i++)
                {
                    inputIndexes[i] = problemExample.InputIndexes[i * problemExample.BatchSize];
                }

                for (int i = 0; i < outputIndexes.Length; i++)
                {
                    outputIndexes[i] = problemExample.OutputIndexes[i * problemExample.BatchSize];
                }

                yield return new ProblemExample(new ProblemInput(inputIndexes), new ProblemOutput(outputIndexes, problemExample.BitsUsedMasks[batchIndex]));
            }
        }
    }

    public ProblemCollection CreateSubset(ProblemExample[] subsetExamples) => new(subsetExamples.Chunk(MaxBatchSize).Select(CreateBatch).ToArray(), _vectors, _vectorCount, MaxBatchSize);

    private static ProblemExampleBatch CreateBatch(ProblemExample[] problemExamples)
    {
        int[] inputIndexes = new int[problemExamples[0].Input.Indexes.Length * problemExamples.Length];
        int[] outputIndexes = new int[problemExamples[0].Output.Indexes.Length * problemExamples.Length];
        Vector256<int>[] bitsUsedMasks = new Vector256<int>[problemExamples.Length];

        for (int inputIndex = 0; inputIndex < problemExamples[0].Input.Indexes.Length; inputIndex++)
        {
            for (int problemExampleIndex = 0; problemExampleIndex < problemExamples.Length; problemExampleIndex++)
            {
                inputIndexes[inputIndex * problemExamples.Length + problemExampleIndex] = problemExamples[problemExampleIndex].Input.Indexes[inputIndex];
            }
        }

        for (int outputIndex = 0; outputIndex < problemExamples[0].Output.Indexes.Length; outputIndex++)
        {
            for (int problemExampleIndex = 0; problemExampleIndex < problemExamples.Length; problemExampleIndex++)
            {
                outputIndexes[outputIndex * problemExamples.Length + problemExampleIndex] = problemExamples[problemExampleIndex].Output.Indexes[outputIndex];
            }
        }

        for (int i = 0; i < bitsUsedMasks.Length; i++)
        {
            bitsUsedMasks[i] = problemExamples[i].Output.MaskBitsUsed;
        }

        return new ProblemExampleBatch(inputIndexes, outputIndexes, bitsUsedMasks);
    }
}
