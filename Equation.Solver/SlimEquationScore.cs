namespace Equation.Solver;

internal readonly record struct SlimEquationScore(int WrongBits) : IComparable<SlimEquationScore>
{
    public int CompareTo(SlimEquationScore other)
    {
        return WrongBits.CompareTo(other.WrongBits);
    }

    public static bool operator <(SlimEquationScore left, SlimEquationScore right)
    {
        return left.WrongBits < right.WrongBits;
    }

    public static bool operator >(SlimEquationScore left, SlimEquationScore right)
    {
        return left.WrongBits > right.WrongBits;
    }

    public static bool operator <(SlimEquationScore left, EquationScore right)
    {
        return left.WrongBits < right.WrongBits;
    }

    public static bool operator >(SlimEquationScore left, EquationScore right)
    {
        return left.WrongBits > right.WrongBits;
    }

    public EquationScore ToFullScore(EquationValues equationValues, ProblemEquation equation)
    {
        (int sequentialNandGates, int nandCount) = CalculateMaxLength(equationValues.StaticResultSize, equation.OutputSize, equation.NandOperators);
        return new EquationScore(WrongBits, sequentialNandGates, nandCount);
    }

    private (int sequentialNandGates, int nandCount) CalculateMaxLength(int staticResultSize, int outputCount, ReadOnlySpan<NandOperator> nandOperators)
    {
        var nodesUsed = new HashSet<int>();
        var nodesToCheck = new Stack<NandDistance>();
        int startNodes = outputCount;
        for (int i = 0; i < startNodes; i++)
        {
            AddIndexesToStack(staticResultSize, 0, nodesToCheck, nandOperators[nandOperators.Length - i - 1], nodesUsed);
        }

        int maxDepth = 1;
        while (nodesToCheck.Count > 0)
        {
            NandDistance distance = nodesToCheck.Pop();
            maxDepth = Math.Max(maxDepth, distance.Distance);

            AddIndexesToStack(staticResultSize, distance.Distance, nodesToCheck, nandOperators[distance.NandIndex], nodesUsed);
        }

        return (maxDepth, nodesUsed.Count);
    }

    private static void AddIndexesToStack(int staticResultSize, int depth, Stack<NandDistance> nodes, NandOperator nandOperator, HashSet<int> nodesUsed)
    {
        int leftIndex = nandOperator.LeftValueIndex - staticResultSize;
        if (leftIndex > 0)
        {
            nodes.Push(new NandDistance(depth + 1, leftIndex));
            nodesUsed.Add(leftIndex);
        }

        int rightIndex = nandOperator.RightValueIndex - staticResultSize;
        if (rightIndex > 0)
        {
            nodes.Push(new NandDistance(depth + 1, rightIndex));
            nodesUsed.Add(rightIndex);
        }
    }

    private readonly record struct NandDistance(int Distance, int NandIndex);
}
