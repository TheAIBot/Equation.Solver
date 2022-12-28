namespace Equation.Solver;

internal sealed class EquationValues
{
    public int[] AllValues;
    private readonly Memory<int> _constants;
    private readonly Memory<int> _parameters;
    public Memory<int> OperatorResults;

    public int StaticResultSize => _constants.Length + _parameters.Length;

    public EquationValues(int parameterCount, int operatorCount)
    {
        const int constantsCount = 2;
        AllValues = new int[constantsCount + parameterCount + operatorCount];
        _constants = AllValues.AsMemory(0, constantsCount);
        _constants.Span[0] = 0;
        _constants.Span[1] = -1;
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
