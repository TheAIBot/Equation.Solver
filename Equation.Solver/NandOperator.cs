namespace Equation.Solver;

internal readonly struct NandOperator
{
    public readonly int _leftValueIndex;
    public readonly int _rightValueIndex;

    public NandOperator(int leftValueIndex, int rightValueIndex)
    {
        _leftValueIndex = leftValueIndex;
        _rightValueIndex = rightValueIndex;
    }
}