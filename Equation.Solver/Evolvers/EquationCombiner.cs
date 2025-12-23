namespace Equation.Solver.Evolvers;

internal sealed class EquationCombiner
{
    private FastResetBoolArray _selectedOutputsOperatorsUsed;
    private FastResetBoolArray _outputSelection;
    private readonly int[] _oldToNewIndex;

    public EquationCombiner(int operatorCount, int outputCount)
    {
        _selectedOutputsOperatorsUsed = new FastResetBoolArray(operatorCount);
        _outputSelection = new FastResetBoolArray(outputCount);
        _oldToNewIndex = new int[_selectedOutputsOperatorsUsed.Length];
    }

    public bool CombineEquations(Random random,
                                 int inputParameterCount,
                                 ProblemEquation parentA,
                                 ProblemEquation parentB,
                                 ProblemEquation child)
    {
        // There is a flaw with this approach. If a parents output uses another of its outputs then
        // both outputs will be copied to the child even if both outputs should not be copied from
        // the parent. The last that is copied from is the one whose operator will reside in the output.
        // This may cause issues with some children but hopefully equations with outputs that depend on
        // each other will eventually be evolved out.

        ArgumentOutOfRangeException.ThrowIfNotEqual(parentA.OutputSize, parentB.OutputSize);
        ArgumentOutOfRangeException.ThrowIfNotEqual(parentA.OutputSize, child.OutputSize);

        if (parentA.OperatorsUsedCount - parentA.OutputSize + parentB.OperatorsUsedCount - parentB.OutputSize > child.NandOperators.Length - child.OutputSize)
        {
            return false;
        }

        int[] oldToNewIndex = _oldToNewIndex;
        _selectedOutputsOperatorsUsed.Clear();
        _outputSelection.Clear();
        Array.Clear(oldToNewIndex);


        for (int i = 0; i < _outputSelection.Length; i++)
        {
            _outputSelection[i] = random.Next(0, 2) == 1;
        }

        int nonOuputOperatorCount = CalculateOutputOperatorsUsed(inputParameterCount, parentA, _selectedOutputsOperatorsUsed, _outputSelection, true);

        int newNandIndex = 0;
        newNandIndex = CopyUsedOperatorsFromParentToChild(inputParameterCount, parentA, child, _selectedOutputsOperatorsUsed, _outputSelection, oldToNewIndex, newNandIndex);

        Array.Clear(oldToNewIndex);
        _selectedOutputsOperatorsUsed.Clear();
        nonOuputOperatorCount = CalculateOutputOperatorsUsed(inputParameterCount, parentB, _selectedOutputsOperatorsUsed, _outputSelection, false);
        newNandIndex = CopyUsedOperatorsFromParentToChild(inputParameterCount, parentB, child, _selectedOutputsOperatorsUsed, _outputSelection, oldToNewIndex, newNandIndex);

        child.RecalculateOperatorsUsed(inputParameterCount);
        return true;
    }

    private static int CalculateOutputOperatorsUsed(int inputParameterCount,
                                                    ProblemEquation parentA,
                                                    FastResetBoolArray selectedOutputsOperatorsUsed,
                                                    FastResetBoolArray outputSelection,
                                                    bool valueSignalsUse)
    {
        for (int i = 0; i < parentA.OutputSize; i++)
        {
            if (outputSelection[i] != valueSignalsUse)
            {
                continue;
            }

            selectedOutputsOperatorsUsed[selectedOutputsOperatorsUsed.Length - parentA.OutputSize + i] = true;
        }

        int totalOperatorsUsed = ProblemEquation.CalculateRemainingOperatorsUsed(inputParameterCount, parentA.NandOperators, selectedOutputsOperatorsUsed);
        return totalOperatorsUsed - outputSelection.Length;
    }

    private static int CopyUsedOperatorsFromParentToChild(int inputParameterCount,
                                                          ProblemEquation parent,
                                                          ProblemEquation child,
                                                          FastResetBoolArray selectedOutputsOperatorsUsed,
                                                          FastResetBoolArray outputSelection,
                                                          int[] oldToNewIndex,
                                                          int newNandIndex)
    {
        for (int i = 0; i < selectedOutputsOperatorsUsed.Length - outputSelection.Length; i++)
        {
            if (!selectedOutputsOperatorsUsed[i])
            {
                continue;
            }

            NandOperator nandOperator = parent.NandOperators[i];
            oldToNewIndex[i] = newNandIndex;


            if (nandOperator.LeftValueIndex >= inputParameterCount)
            {
                nandOperator = new NandOperator(oldToNewIndex[nandOperator.LeftValueIndex - inputParameterCount] + inputParameterCount, nandOperator.RightValueIndex);
            }

            if (nandOperator.RightValueIndex >= inputParameterCount)
            {
                nandOperator = new NandOperator(nandOperator.LeftValueIndex, oldToNewIndex[nandOperator.RightValueIndex - inputParameterCount] + inputParameterCount);
            }

            child.NandOperators[newNandIndex] = nandOperator;
            newNandIndex++;
        }

        for (int i = 0; i < parent.OutputSize; i++)
        {
            int operatorIndex = selectedOutputsOperatorsUsed.Length - parent.OutputSize + i;
            if (!selectedOutputsOperatorsUsed[operatorIndex])
            {
                continue;
            }

            NandOperator nandOperator = parent.NandOperators[operatorIndex];
            oldToNewIndex[operatorIndex] = operatorIndex;


            if (nandOperator.LeftValueIndex >= inputParameterCount)
            {
                nandOperator = new NandOperator(oldToNewIndex[nandOperator.LeftValueIndex - inputParameterCount] + inputParameterCount, nandOperator.RightValueIndex);
            }

            if (nandOperator.RightValueIndex >= inputParameterCount)
            {
                nandOperator = new NandOperator(nandOperator.LeftValueIndex, oldToNewIndex[nandOperator.RightValueIndex - inputParameterCount] + inputParameterCount);
            }

            child.NandOperators[operatorIndex] = nandOperator;
        }

        return newNandIndex;
    }
}