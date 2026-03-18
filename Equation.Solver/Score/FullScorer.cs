namespace Equation.Solver.Score;

internal sealed class FullScorer
{
    private readonly HashSet<int> _nodesUsed = new HashSet<int>();
    private readonly Stack<NandDistance> _nodesToCheck = new Stack<NandDistance>();

    public EquationScore ToFullScore(SlimEquationScore slimScore, EquationValues equationValues, ProblemEquation equation)
    {
        (int sequentialNandGates, int nandCount) = CalculateMaxLength(equationValues.InputParameterCount, equation.OutputSize, equation.NandOperators);
        return new EquationScore(slimScore.WrongBits, sequentialNandGates, nandCount);
    }

    private (int sequentialNandGates, int nandCount) CalculateMaxLength(int inputParameterCount, int outputCount, ReadOnlySpan<NandOperator> nandOperators)
    {
        var nodesUsed = _nodesUsed;
        nodesUsed.Clear();
        var nodesToCheck = _nodesToCheck;
        nodesToCheck.Clear();
        int startNodes = outputCount;
        for (int i = 0; i < startNodes; i++)
        {
            AddIndexesToStack(inputParameterCount, 0, nodesToCheck, nandOperators[nandOperators.Length - i - 1], nodesUsed);
        }

        int maxDepth = 1;
        while (nodesToCheck.Count > 0)
        {
            NandDistance distance = nodesToCheck.Pop();
            maxDepth = Math.Max(maxDepth, distance.Distance);

            AddIndexesToStack(inputParameterCount, distance.Distance, nodesToCheck, nandOperators[distance.NandIndex], nodesUsed);
        }

        return (maxDepth, nodesUsed.Count);
    }

    private static void AddIndexesToStack(int inputParameterCount, int depth, Stack<NandDistance> nodes, NandOperator nandOperator, HashSet<int> nodesUsed)
    {
        int leftIndex = nandOperator.LeftValueIndex - inputParameterCount;
        if (leftIndex > 0 && nodesUsed.Add(leftIndex))
        {
            nodes.Push(new NandDistance(depth + 1, leftIndex));
        }

        int rightIndex = nandOperator.RightValueIndex - inputParameterCount;
        if (rightIndex > 0 && nodesUsed.Add(rightIndex))
        {
            nodes.Push(new NandDistance(depth + 1, rightIndex));
        }
    }

    private readonly record struct NandDistance(int Distance, int NandIndex);
}
