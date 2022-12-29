using System.Runtime.Intrinsics;

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

    public Vector256<int> Calculate(Vector256<int>[] values)
    {
        Vector256<int> leftValue = values[_leftValueIndex];
        Vector256<int> rightValue = values[_rightValueIndex];

        return ~(leftValue & rightValue);
    }
}