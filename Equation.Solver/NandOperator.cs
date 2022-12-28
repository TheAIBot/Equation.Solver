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

    public int Calculate(ProblemEquation equation)
    {
        int leftValue = equation.GetResult(_leftValueIndex);
        int rightValue = equation.GetResult(_rightValueIndex);

        return ~(leftValue & rightValue);
    }
}