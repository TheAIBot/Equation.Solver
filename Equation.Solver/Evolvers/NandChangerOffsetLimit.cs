namespace Equation.Solver.Evolvers;

internal sealed class NandChangerOffsetLimit
{
    /// <summary>
    /// Randomly changes the inputs of randomly selected operators.
    /// </summary>
    /// <returns>False if change has no effect on output and True if it may have an effect.</returns>
    public bool RandomizeSmallPartOfEquation(Random random, ProblemEquation equation, EquationValues equationValues, int operatorCountToRandomize)
    {
        const float maxOffsetPercent = 0.02f;
        const float startOperatorsCanReadAllInputPercent = 0.2f;
        int maxOffset = (int)(equation.NandOperators.Length * maxOffsetPercent);
        int maxOperatorIndexCanReadAllInput = (int)(equation.NandOperators.Length * startOperatorsCanReadAllInputPercent);

        bool wasAnyChangedOperatorUsed = false;
        Span<NandOperator> operators = equation.NandOperators;
        int inputParameterCount = equationValues.InputParameterCount;
        for (int i = 0; i < operatorCountToRandomize; i++)
        {
            int operatorIndex = random.Next(0, operators.Length);
            wasAnyChangedOperatorUsed = Randomize(random, equation, maxOffset, maxOperatorIndexCanReadAllInput, wasAnyChangedOperatorUsed, operators, inputParameterCount, operatorIndex);
        }

        if (!wasAnyChangedOperatorUsed)
        {
            return false;
        }

        equation.RecalculateOperatorsUsed(inputParameterCount);
        return true;
    }

    public bool RandomizeWholeEquation(Random random, ProblemEquation equation, EquationValues equationValues)
    {
        const float maxOffsetPercent = 0.1f;
        const float startOperatorsCanReadAllInputPercent = 0.2f;
        int maxOffset = (int)(equation.NandOperators.Length * maxOffsetPercent);
        int maxOperatorIndexCanReadAllInput = (int)(equation.NandOperators.Length * startOperatorsCanReadAllInputPercent);

        Span<NandOperator> operators = equation.NandOperators;
        int inputParameterCount = equationValues.InputParameterCount;
        for (int i = 0; i < operators.Length; i++)
        {
            Randomize(random, equation, maxOffset, maxOperatorIndexCanReadAllInput, false, operators, inputParameterCount, i);
        }

        equation.RecalculateOperatorsUsed(inputParameterCount);
        return true;
    }

    private static bool Randomize(Random random, ProblemEquation equation, int maxOffset, int maxOperatorIndexCanReadAllInput, bool wasAnyChangedOperatorUsed, Span<NandOperator> operators, int inputParameterCount, int operatorIndex)
    {
        wasAnyChangedOperatorUsed |= equation.OperatorsUsed[operatorIndex];

        if (operatorIndex <= maxOperatorIndexCanReadAllInput)
        {
            int leftValueIndex = random.Next(0, inputParameterCount + operatorIndex);
            int rightValueIndex = random.Next(0, inputParameterCount + operatorIndex);
            operators[operatorIndex] = new NandOperator(leftValueIndex, rightValueIndex);
        }
        else
        {
            int maxChange = Math.Min(operatorIndex + inputParameterCount, maxOffset);
            int leftOffset = random.Next(1, maxChange);
            int rightOffset = random.Next(1, maxChange);
            operators[operatorIndex] = new NandOperator(operatorIndex + inputParameterCount - leftOffset, operatorIndex + inputParameterCount - rightOffset);
        }

        return wasAnyChangedOperatorUsed;
    }
}