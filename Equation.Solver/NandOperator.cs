namespace Equation.Solver;

internal readonly struct NandOperator
{
    private readonly int _leftValueIndex;
    private readonly int _rightValueIndex;

    public NandOperator(int leftValueIndex, int rightValueIndex)
    {
        _leftValueIndex = leftValueIndex;
        _rightValueIndex = rightValueIndex;
    }

    public int Calculate(int[] values)
    {
        int leftValue = values[_leftValueIndex];
        int rightValue = values[_rightValueIndex];

        return ~(leftValue & rightValue);
    }
}