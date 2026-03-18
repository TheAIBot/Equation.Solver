using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal unsafe readonly struct ProblemInput
{
    private readonly Vector256<int>* _inputs;
    private readonly int _length;


    public readonly Vector256<int>* Inputs => _inputs;
    public readonly int Count => _length;

    public ProblemInput(Vector256<int>[] inputs)
    {
        _inputs = (Vector256<int>*)NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<int>) * inputs.Length), (nuint)sizeof(Vector256<int>));
        _length = inputs.Length;

        for (int i = 0; i < inputs.Length; i++)
        {
            _inputs[i] = inputs[i];
        }
    }
}
