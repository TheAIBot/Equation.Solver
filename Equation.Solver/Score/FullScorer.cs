namespace Equation.Solver.Score;

internal sealed class FullScorer
{
    private readonly int[] _nodesToCheck;
    private readonly int[] _nodeDistances;
    private readonly int[] _nodeIndexes;
    private const int _nodeIndexNotInNodesToCheck = -1;
    private const int _nodeNotSeenBeforeDistance = -1;

    public FullScorer(int operatorCount)
    {
        _nodesToCheck = new int[operatorCount];
        _nodeDistances = new int[operatorCount];
        _nodeIndexes = new int[operatorCount];
    }

    public EquationScore ToFullScore(SlimEquationScore slimScore, EquationValues equationValues, ProblemEquation equation)
    {
        (int sequentialNandGates, int nandCount) = CalculateMaxLength(equationValues.InputParameterCount, equation.OutputSize, equation.NandOperators);
        return new EquationScore(slimScore.WrongBits, sequentialNandGates, nandCount);
    }

    private (int sequentialNandGates, int nandCount) CalculateMaxLength(int inputParameterCount, int outputCount, ReadOnlySpan<NandOperator> nandOperators)
    {
        var nodesToCheck = _nodesToCheck;
        var nodeDistances = _nodeDistances;
        var nodeIndexes = _nodeIndexes;
        Array.Fill(nodeDistances, _nodeNotSeenBeforeDistance);

        int nodesToCheckCount = 0;
        int uniqueSeenNodes = 0;

        for (int i = 0; i < outputCount; i++)
        {
            AddNandDependenciesToStack(inputParameterCount, outputCount, 1, nodesToCheck, nandOperators[nandOperators.Length - i - 1], nodeDistances, nodeIndexes, ref uniqueSeenNodes, ref nodesToCheckCount);
        }

        int maxDepth = 1;
        while (nodesToCheckCount > 0)
        {
            int nodeIndex = nodesToCheck[nodesToCheckCount - 1];
            nodesToCheckCount--;

            nodeIndexes[nodeIndex] = _nodeIndexNotInNodesToCheck;
            int nodeDistance = nodeDistances[nodeIndex];

            maxDepth = Math.Max(maxDepth, nodeDistance);

            AddNandDependenciesToStack(inputParameterCount, outputCount, nodeDistance, nodesToCheck, nandOperators[nodeIndex], nodeDistances, nodeIndexes, ref uniqueSeenNodes, ref nodesToCheckCount);
        }

        // The nand operators on the outputs are not counted in the above which is why
        // they are added here
        return (maxDepth, uniqueSeenNodes + outputCount);
    }

    private static void AddNandDependenciesToStack(int inputParameterCount,
                                                   int outputCount,
                                                   int depth,
                                                   int[] nodes,
                                                   NandOperator nandOperator,
                                                   int[] nodeDistances,
                                                   int[] nodeIndexes,
                                                   ref int uniqueSeenNodes,
                                                   ref int nodesToCheckCount)
    {
        AddNandDependencyToStack(inputParameterCount, outputCount, depth, nodes, nandOperator.LeftValueIndex, nodeDistances, nodeIndexes, ref uniqueSeenNodes, ref nodesToCheckCount);
        AddNandDependencyToStack(inputParameterCount, outputCount, depth, nodes, nandOperator.RightValueIndex, nodeDistances, nodeIndexes, ref uniqueSeenNodes, ref nodesToCheckCount);
    }

    private static void AddNandDependencyToStack(int inputParameterCount,
                                                 int outputCount,
                                                 int depth,
                                                 int[] nodes,
                                                 int nandDependencyIndex,
                                                 int[] nodeDistances,
                                                 int[] nodeIndexes,
                                                 ref int uniqueSeenNodes,
                                                 ref int nodesToCheckCount)
    {
        nandDependencyIndex = nandDependencyIndex - inputParameterCount;
        if (nandDependencyIndex < 0)
        {
            return;
        }

        int existingNodeDistance = nodeDistances[nandDependencyIndex];
        if (existingNodeDistance == _nodeNotSeenBeforeDistance)
        {
            nodes[nodesToCheckCount] = nandDependencyIndex;
            nodeDistances[nandDependencyIndex] = depth + 1;
            nodeIndexes[nandDependencyIndex] = nodesToCheckCount;
            if (nandDependencyIndex < nodes.Length - outputCount)
            {
                uniqueSeenNodes++;
            }
            nodesToCheckCount++;
            return;
        }

        if (existingNodeDistance >= depth + 1)
        {
            return;
        }

        int existingNodeIndex = nodeIndexes[nandDependencyIndex];
        if (existingNodeIndex == _nodeIndexNotInNodesToCheck)
        {
            existingNodeIndex = nodesToCheckCount;
            nodeIndexes[nandDependencyIndex] = nodesToCheckCount;
            nodesToCheckCount++;
        }

        nodes[existingNodeIndex] = nandDependencyIndex;
        nodeDistances[nandDependencyIndex] = depth + 1;
    }
}
