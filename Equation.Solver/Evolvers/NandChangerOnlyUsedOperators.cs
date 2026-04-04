namespace Equation.Solver.Evolvers;

internal sealed class NandChangerOnlyUsedOperators
{
    private readonly List<int> _usedIndexes = [];

    /// <summary>
    /// Randomly changes the inputs of randomly selected operators.
    /// </summary>
    /// <returns>False if change has no effect on output and True if it may have an effect.</returns>
    public bool RandomizeSmallPartOfEquation(Random random, ProblemEquation equation, EquationValues equationValues, int operatorCountToRandomize)
    {
        _usedIndexes.Clear();
        for (int i = 0; i < equation.OperatorsUsed.Length; i++)
        {
            if (equation.OperatorsUsed[i])
            {
                _usedIndexes.Add(i);
            }
        }

        Span<NandOperator> operators = equation.NandOperators;
        int inputParameterCount = equationValues.InputParameterCount;
        for (int i = 0; i < operatorCountToRandomize; i++)
        {
            int operatorIndex = _usedIndexes[random.Next(0, _usedIndexes.Count)];
            int leftValueIndex = random.Next(0, inputParameterCount + operatorIndex);
            int rightValueIndex = random.Next(0, inputParameterCount + operatorIndex);
            operators[operatorIndex] = new NandOperator(leftValueIndex, rightValueIndex);
        }

        equation.RecalculateOperatorsUsed(inputParameterCount);
        return true;
    }
}
