namespace Equation.Solver;

internal sealed class ProblemEquation
{
    private readonly int[] _allValues;
    private readonly Memory<int> _constants;
    private readonly Memory<int> _parameters;
    private readonly Memory<int> _operatorResults;
    private readonly NandOperator[] _nandOperators;
    private readonly int _outputSize;
    public Span<NandOperator> NandOperators => _nandOperators;
    public int StaticResultSize => _constants.Length + _parameters.Length;

    public ProblemEquation(int parameterCount, int operatorCount, int outputSize)
    {
        const int constantsCount = 2;
        _allValues = new int[constantsCount + parameterCount + operatorCount];
        _constants = _allValues.AsMemory(0, constantsCount);
        _constants.Span[0] = 0;
        _constants.Span[1] = -1;
        _parameters = _allValues.AsMemory(constantsCount, parameterCount);
        _operatorResults = _allValues.AsMemory(constantsCount + parameterCount, operatorCount);
        _nandOperators = new NandOperator[operatorCount];
        _outputSize = outputSize;
    }

    public int GetResult(int index)
    {
        return _allValues[index];
    }

    public ReadOnlySpan<int> Calculate(ReadOnlySpan<int> parameters)
    {
        if (parameters.Length != _parameters.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters));
        }
        parameters.CopyTo(_parameters.Span);

        Span<int> results = _operatorResults.Span;
        for (int i = 0; i < _nandOperators.Length; i++)
        {
            results[i] = _nandOperators[i].Calculate(this);
        }

        return results.Slice(results.Length - _outputSize, _outputSize);
    }

    public ProblemEquation Copy()
    {
        var copy = new ProblemEquation(_parameters.Length, _nandOperators.Length, _outputSize);
        copy.CopyFrom(this);

        return copy;
    }

    public void CopyFrom(ProblemEquation copyFrom)
    {
        Array.Copy(copyFrom._nandOperators, _nandOperators, copyFrom._nandOperators.Length);
    }
}
