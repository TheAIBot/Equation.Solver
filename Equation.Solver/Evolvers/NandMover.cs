using System.Diagnostics;

namespace Equation.Solver.Evolvers;

internal sealed class NandMover
{
    private readonly bool[] _operatorsUsed;
    private readonly NandMoveConstraint[] _nandMoveConstraints;
    private readonly Stack<int> _nodesToCheck;
    private readonly NandIndexMoveConstraint[] _nandsUsedMoveConstraints;

    public NandMover(int staticResultSize, int operatorCount)
    {
        _operatorsUsed = new bool[staticResultSize + operatorCount];
        _nandMoveConstraints = new NandMoveConstraint[_operatorsUsed.Length];
        _nodesToCheck = new Stack<int>();
        _nandsUsedMoveConstraints = new NandIndexMoveConstraint[_operatorsUsed.Length];
    }

    public void MoveRandomNandOperator(Random random, int staticResultSize, int outputCount, Span<NandOperator> operators)
    {
        NandMoveConstraints nandIndexMoveConstraints = GetMoveConstraintsOfAllUsedNands(staticResultSize, outputCount, operators);
        if (nandIndexMoveConstraints.NandIndexMoveConstraints.Length == 0)
        {
            return;
        }

        int moveConstraintToMove = random.Next(nandIndexMoveConstraints.NandIndexMoveConstraints.Length);
        TryMoveOperator(random, staticResultSize, operators, nandIndexMoveConstraints.NandIndexMoveConstraints, moveConstraintToMove, nandIndexMoveConstraints.OperatorsUsed);
    }

    private NandMoveConstraints GetMoveConstraintsOfAllUsedNands(int staticResultSize, int outputCount, ReadOnlySpan<NandOperator> nandOperators)
    {
        var nodesUsed = _operatorsUsed;
        Array.Fill(nodesUsed, false);
        var nandMoveConstraints = _nandMoveConstraints;
        Array.Fill(nandMoveConstraints, new NandMoveConstraint(int.MinValue, int.MaxValue));
        int nodesUsedCount = 0;
        var nodesToCheck = _nodesToCheck;
        nodesToCheck.Clear();
        int startNodes = outputCount;
        for (int i = 0; i < startNodes; i++)
        {
            AddIndexesToStack(staticResultSize, nodesToCheck, nandOperators[nandOperators.Length - i - 1], nodesUsed, ref nodesUsedCount, nandMoveConstraints);
        }

        while (nodesToCheck.Count > 0)
        {
            int nandIndex = nodesToCheck.Pop();
            NandOperator nandOperator = nandOperators[nandIndex - staticResultSize];

            // Calculation goes from left to right so can't move left of operator  value it uses
            nandMoveConstraints[nandIndex].MaxExclusiveLowerBound = Math.Max(nandOperator.LeftValueIndex, nandOperator.RightValueIndex);

            AddIndexesToStack(staticResultSize, nodesToCheck, nandOperator, nodesUsed, ref nodesUsedCount, nandMoveConstraints);
        }

        Span<NandIndexMoveConstraint> nandsUsedMoveConstraints = _nandsUsedMoveConstraints.AsSpan(0, nodesUsedCount);
        int nodeIndexesUsedFreeIndex = 0;
        for (int i = 0; i < nodesUsed.Length; i++)
        {
            if (nodesUsed[i])
            {
                nandsUsedMoveConstraints[nodeIndexesUsedFreeIndex++] = new NandIndexMoveConstraint(i, nandMoveConstraints[i]);
            }
        }

        return new NandMoveConstraints(nandsUsedMoveConstraints, nodesUsed);
    }

    private static void AddIndexesToStack(int staticResultSize, Stack<int> nodes, NandOperator nandOperator, bool[] nodesUsed, ref int nodesUsedCount, NandMoveConstraint[] nandMoveConstraints)
    {
        int leftIndex = nandOperator.LeftValueIndex - staticResultSize;
        if (leftIndex > 0)
        {
            AddOrUpdateMovConstraint(nodes, nandOperator.LeftValueIndex, nodesUsed, ref nodesUsedCount, nandMoveConstraints);
        }

        int rightIndex = nandOperator.RightValueIndex - staticResultSize;
        if (rightIndex > 0)
        {
            AddOrUpdateMovConstraint(nodes, nandOperator.RightValueIndex, nodesUsed, ref nodesUsedCount, nandMoveConstraints);
        }
    }

    private static void AddOrUpdateMovConstraint(Stack<int> nodes, int nandOperatorIndex, bool[] nodesUsed, ref int nodesUsedCount, NandMoveConstraint[] nandMoveConstraints)
    {
        if (!nodesUsed[nandOperatorIndex])
        {
            nodesUsed[nandOperatorIndex] = true;
            nodesUsedCount++;
            nodes.Push(nandOperatorIndex);
        }

        // Calculation goes from left to right so operator can never move beyond any operator that uses it
        nandMoveConstraints[nandOperatorIndex].MinExclusiveUpperBound = Math.Min(nandMoveConstraints[nandOperatorIndex].MinExclusiveUpperBound, nandOperatorIndex);
    }

    private static void TryMoveOperator(Random random,
                                        int staticResultSize,
                                        Span<NandOperator> operators,
                                        Span<NandIndexMoveConstraint> nandIndexMoveConstraints,
                                        int moveConstraintToMove,
                                        bool[] operatorsUsed)
    {
        NandIndexMoveConstraint moveConstraint = nandIndexMoveConstraints[moveConstraintToMove];
        int actualMinMoveIndex = moveConstraint.MoveConstraint.MaxExclusiveLowerBound + 1;
        // Not allowed to move into parameters as that is not operator space
        actualMinMoveIndex = Math.Max(actualMinMoveIndex, staticResultSize);

        int actualMaxMoveIndex = moveConstraint.MoveConstraint.MinExclusiveUpperBound - 1;

        int possiblePositions = actualMaxMoveIndex - actualMinMoveIndex + 1;
        // No need to do rest if operator can not be moved.
        // one possible position is its current position.
        if (possiblePositions <= 1)
        {
            return;
        }

        // Moveable space is defined by actualMinMoveIndex and actualMaxMoveIndex.
        // Can not move operator op top of another used operator.
        // This look finds all used operators within the moveable space
        // and reduces the possible positions accordingly.
        for (int i = actualMinMoveIndex; i <= actualMaxMoveIndex; i++)
        {
            if (operatorsUsed[i])
            {
                possiblePositions--;
            }
        }

        // All positions within the moveable space is used
        if (possiblePositions <= 0)
        {
            return;
        }

        int moveableIndex = random.Next(0, possiblePositions);
        int moveFrom = moveConstraint.NandIndex;
        int moveTo = -1;
        for (int i = actualMinMoveIndex; i <= actualMaxMoveIndex; i++)
        {
            if (!operatorsUsed[i])
            {
                if (moveableIndex-- == 0)
                {
                    moveTo = i;
                    operators[moveTo - staticResultSize] = operators[moveFrom - staticResultSize];
                }
            }
        }

        Debug.Assert(moveTo != -1, "Logic for moveable space is invalid. The expected amount of available space was not found.");
        Debug.Assert(moveTo < operators.Length + staticResultSize);

        // Need to update all operators that points to the move operator
        // so they now use the operators new index
        for (int i = actualMaxMoveIndex; i < operators.Length; i++)
        {
            if (operators[i].LeftValueIndex == moveFrom)
            {
                operators[i] = new NandOperator(moveTo, operators[i].RightValueIndex);
            }

            if (operators[i].RightValueIndex == moveFrom)
            {
                operators[i] = new NandOperator(operators[i].LeftValueIndex, moveTo);
            }
        }
    }

    private record struct NandMoveConstraint(int MaxExclusiveLowerBound, int MinExclusiveUpperBound);
    private record struct NandIndexMoveConstraint(int NandIndex, NandMoveConstraint MoveConstraint);
    private readonly ref struct NandMoveConstraints
    {
        public readonly Span<NandIndexMoveConstraint> NandIndexMoveConstraints;
        public readonly bool[] OperatorsUsed;

        public NandMoveConstraints(Span<NandIndexMoveConstraint> nandIndexMoveConstraints, bool[] operatorsUsed)
        {
            NandIndexMoveConstraints = nandIndexMoveConstraints;
            OperatorsUsed = operatorsUsed;
        }
    }
}
