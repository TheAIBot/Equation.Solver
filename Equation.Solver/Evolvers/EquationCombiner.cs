namespace Equation.Solver.Evolvers;

internal sealed class EquationCombiner
{
    private FastResetBoolArray _selectedOutputsOperatorsUsedA;
    private FastResetBoolArray _selectedOutputsOperatorsUsedB;
    private readonly int[] _oldToNewIndex;
    private readonly int _outputCount;

    public EquationCombiner(int operatorCount, int outputCount)
    {
        _selectedOutputsOperatorsUsedA = new FastResetBoolArray(operatorCount);
        _selectedOutputsOperatorsUsedB = new FastResetBoolArray(operatorCount);
        _oldToNewIndex = new int[_selectedOutputsOperatorsUsedA.Length];
        _outputCount = outputCount;
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

        _selectedOutputsOperatorsUsedA.Clear();
        _selectedOutputsOperatorsUsedB.Clear();

        int nonOuputOperatorCount = CalculateOutputOperatorsUsed(random, inputParameterCount, parentA, _selectedOutputsOperatorsUsedA, parentB, _selectedOutputsOperatorsUsedB, _outputCount);
        if (nonOuputOperatorCount > child.NandOperators.Length - child.OutputSize)
        {
            return false;
        }

        int[] oldToNewIndex = _oldToNewIndex;
        Array.Clear(oldToNewIndex);

        int newNandIndex = 0;
        newNandIndex = CopyUsedOperatorsFromParentToChild(inputParameterCount, parentA, child, _selectedOutputsOperatorsUsedA, _outputCount, oldToNewIndex, newNandIndex);

        Array.Clear(oldToNewIndex);
        newNandIndex = CopyUsedOperatorsFromParentToChild(inputParameterCount, parentB, child, _selectedOutputsOperatorsUsedB, _outputCount, oldToNewIndex, newNandIndex);

        child.RecalculateOperatorsUsed(inputParameterCount);
        return true;
    }

    private static int CalculateOutputOperatorsUsed(Random random,
                                                    int inputParameterCount,
                                                    ProblemEquation parentA,
                                                    FastResetBoolArray selectedOutputsOperatorsUsedA,
                                                    ProblemEquation parentB,
                                                    FastResetBoolArray selectedOutputsOperatorsUsedB,
                                                    int outputCount)
    {
        for (int i = 0; i < parentA.OutputSize; i++)
        {
            if (random.Next(0, 2) == 1)
            {
                selectedOutputsOperatorsUsedA[selectedOutputsOperatorsUsedA.Length - parentA.OutputSize + i] = true;
            }
            else
            {
                selectedOutputsOperatorsUsedB[selectedOutputsOperatorsUsedB.Length - parentB.OutputSize + i] = true;
            }
        }

        int totalOperatorsUsed = ProblemEquation.CalculateRemainingOperatorsUsed(inputParameterCount, parentA.NandOperators, selectedOutputsOperatorsUsedA);
        totalOperatorsUsed -= outputCount;
        totalOperatorsUsed += ProblemEquation.CalculateRemainingOperatorsUsed(inputParameterCount, parentB.NandOperators, selectedOutputsOperatorsUsedB);
        totalOperatorsUsed -= outputCount;
        return totalOperatorsUsed;
    }

    private static int CopyUsedOperatorsFromParentToChild(int inputParameterCount,
                                                          ProblemEquation parent,
                                                          ProblemEquation child,
                                                          FastResetBoolArray selectedOutputsOperatorsUsed,
                                                          int outputCount,
                                                          int[] oldToNewIndex,
                                                          int newNandIndex)
    {
        for (int i = 0; i < selectedOutputsOperatorsUsed.Length - outputCount; i++)
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