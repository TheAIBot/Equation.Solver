using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed unsafe class ProblemCollection
{
    private readonly Vector256<int>* _vectors;
    private readonly int _vectorCount;

    public ProblemExample[] Examples { get; }
    public Vector256<int>* Vectors => _vectors;
    public int VectorCount => _vectorCount;

    public ProblemCollection(ProblemExample[] examples, Vector256<int>* vectors, int vectorCount)
    {
        Examples = examples;
        _vectors = vectors;
        _vectorCount = vectorCount;
    }

    public static ProblemCollection Create(ProblemExample[] examples, Vector256<int>[] uniqueVectors)
    {
        Vector256<int>* vectors = (Vector256<int>*)NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<int>) * uniqueVectors.Length),
                                                                             (nuint)sizeof(Vector256<int>));
        for (int i = 0; i < uniqueVectors.Length; i++)
        {
            vectors[i] = uniqueVectors[i];
        }

        return new ProblemCollection(examples, vectors, uniqueVectors.Length);
    }

    public ProblemCollection CreateSubset(ProblemExample[] subsetExamples) => new(subsetExamples, _vectors, _vectorCount);
}
