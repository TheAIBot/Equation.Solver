using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal unsafe sealed class EquationValues
{
    const int constantsCount = 2;
    public readonly Vector256<int>* AllValues;
    public readonly int _size;
    public readonly Vector256<int>* OperatorResults;
    private readonly int _parameterCount;

    public int StaticResultSize => constantsCount + _parameterCount;

    public EquationValues(int parameterCount, int operatorCount)
    {
        _size = constantsCount + parameterCount + operatorCount;
        _parameterCount = parameterCount;
        // For vectorized code, aligned load/stores can be important in order to achieve optimal performance.
        // That's why we align this array by the vectors size so only aligned loads/stores are done.
        AllValues = (Vector256<int>*)NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<int>) * _size), (nuint)sizeof(Vector256<int>));
        AllValues[0] = Vector256<int>.Zero;
        AllValues[1] = Vector256<int>.AllBitsSet;
        OperatorResults = AllValues + constantsCount + parameterCount;
    }

    public void SetParameters(ProblemInput parameters)
    {
        if (parameters.Inputs.Length != _parameterCount)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters));
        }
        var to = new Span<Vector256<int>>(AllValues + constantsCount, _parameterCount);
        parameters.Inputs.CopyTo(to);
    }
}
