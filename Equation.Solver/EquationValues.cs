using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class EquationValues
{
    public Vector256<int>[] AllValues;
    private readonly Memory<Vector256<int>> _constants;
    private readonly Memory<Vector256<int>> _parameters;
    public Memory<Vector256<int>> OperatorResults;

    public int StaticResultSize => _constants.Length + _parameters.Length;

    public EquationValues(int parameterCount, int operatorCount)
    {
        const int constantsCount = 2;
        AllValues = new Vector256<int>[constantsCount + parameterCount + operatorCount];
        _constants = AllValues.AsMemory(0, constantsCount);
        _constants.Span[0] = Vector256<int>.Zero;
        _constants.Span[1] = Vector256<int>.AllBitsSet;
        _parameters = AllValues.AsMemory(constantsCount, parameterCount);
        OperatorResults = AllValues.AsMemory(constantsCount + parameterCount, operatorCount);
    }

    public void SetParameters(ProblemInput parameters)
    {
        if (parameters.Inputs.Length != _parameters.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters));
        }
        parameters.Inputs.CopyTo(_parameters.Span);
    }
}
