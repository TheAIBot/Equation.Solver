using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal sealed class ProblemEquation
{
    private FastResetBoolArray _operatorsUsed;
    private readonly NandOperator[] _nandOperators;
    private readonly int _outputSize;

    public ref FastResetBoolArray OperatorsUsed => ref _operatorsUsed;
    public int OperatorsUsedCount => _operatorsUsed.TrueCount;
    public Span<NandOperator> NandOperators => _nandOperators;
    public int OutputSize => _outputSize;

    public ProblemEquation(int operatorCount, int outputSize)
    {
        _operatorsUsed = new FastResetBoolArray(operatorCount);
        _nandOperators = new NandOperator[operatorCount];
        _outputSize = outputSize;
    }

    public unsafe ReadOnlySpan<Vector256<int>> Calculate(EquationValues equationValues, ProblemExample example)
    {
        Vector256<int>* results = equationValues.OperatorResults;
        int* allValues = (int*)equationValues.AllValues;
        var operatorsUsed = _operatorsUsed;
        int inputCount = example.Input.Count;

        fixed (Vector256<int>* inputs = example.Input.Inputs)
        {
            int* inputsAsInts = (int*)inputs;
            fixed (NandOperator* operators = _nandOperators)
            {
                for (int i = 0; i < _nandOperators.Length; i++)
                {
                    if (!operatorsUsed[i])
                    {
                        continue;
                    }

                    var result = _nandOperators[i].Nand(allValues, inputsAsInts, inputCount);
                    result.StoreAligned((int*)(results + i));
                }
            }
        }


        return new Span<Vector256<int>>(((Vector256<int>*)allValues) + (equationValues._size - _outputSize), _outputSize);
    }

    public void RecalculateOperatorsUsed(int staticResultSize)
    {
        _operatorsUsed.Clear();
        _operatorsUsed.SetRangeTrue(_operatorsUsed.Length - OutputSize, OutputSize, true);

        CalculateRemainingOperatorsUsed(staticResultSize, _nandOperators, _operatorsUsed);
    }

    /// <summary>
    /// Given some operators are used at the end of the <paramref name="operatorsUsed"/> array
    /// this will then mark all other used operators.
    /// </summary>
    /// <returns>Number of operators used.</returns>
    internal static int CalculateRemainingOperatorsUsed(int staticResultSize,
                                                        ReadOnlySpan<NandOperator> operators,
                                                        FastResetBoolArray operatorsUsed)
    {
        int operatorsUsedCount = 0;
        for (int i = operators.Length - 1; i >= 0; i--)
        {
            if (!operatorsUsed[i])
            {
                continue;
            }

            operatorsUsedCount++;
            NandOperator nandOperator = operators[i];
            if (nandOperator.LeftValueIndex >= staticResultSize)
            {
                operatorsUsed[nandOperator.LeftValueIndex - staticResultSize] = true;
            }

            if (nandOperator.RightValueIndex >= staticResultSize)
            {
                operatorsUsed[nandOperator.RightValueIndex - staticResultSize] = true;
            }
        }

        return operatorsUsedCount;
    }

    public ProblemEquation Copy()
    {
        var copy = new ProblemEquation(_nandOperators.Length, _outputSize);
        copy.CopyFrom(this);

        return copy;
    }

    public void CopyFrom(ProblemEquation copyFrom)
    {
        Array.Copy(copyFrom._nandOperators, _nandOperators, copyFrom._nandOperators.Length);
        copyFrom._operatorsUsed.CopyTo(ref _operatorsUsed);
    }
}
