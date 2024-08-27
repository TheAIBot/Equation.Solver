﻿namespace Equation.Solver.Evolvers;

internal sealed class NandChanger
{
    public void RandomizeSmallPartOfEquation(Random random, ProblemEquation equation, EquationValues equationValues, int operatorCountToRandomize)
    {
        bool wasAnyChangedOperatorUsed = false;
        Span<NandOperator> operators = equation.NandOperators;
        int staticResultSize = equationValues.StaticResultSize;
        for (int i = 0; i < operatorCountToRandomize; i++)
        {
            int operatorIndex = random.Next(0, operators.Length);
            wasAnyChangedOperatorUsed |= equation.OperatorsUsed[operatorIndex];
            int leftValueIndex = random.Next(0, staticResultSize + operatorIndex);
            int rightValueIndex = random.Next(0, staticResultSize + operatorIndex);
            operators[operatorIndex] = new NandOperator(leftValueIndex, rightValueIndex);
        }

        if (!wasAnyChangedOperatorUsed)
        {
            return;
        }

        equation.RecalculateOperatorsUsed(staticResultSize);
    }
}
