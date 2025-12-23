using System.Diagnostics;

namespace Equation.Solver.Evolvers;

internal sealed class NandMover
{
    private readonly NandMoveConstraint[] _nandMoveConstraints;
    private readonly NandIndexMoveConstraint[] _nandsUsedMoveConstraints;

    public NandMover(int inputParameterCount, int operatorCount)
    {
        _nandMoveConstraints = new NandMoveConstraint[operatorCount + inputParameterCount];
        _nandsUsedMoveConstraints = new NandIndexMoveConstraint[_nandMoveConstraints.Length];
    }

    public void MoveRandomNandOperator(Random random, int inputParameterCount, int outputCount, Span<NandOperator> operators, FastResetBoolArray operatorsUsed)
    {
        Span<NandIndexMoveConstraint> nandIndexMoveConstraints = GetMoveConstraintsOfAllUsedNands(inputParameterCount, outputCount, operators, operatorsUsed);
        if (nandIndexMoveConstraints.Length == 0)
        {
            return;
        }

        int moveConstraintToMove = random.Next(nandIndexMoveConstraints.Length);
        TryMoveOperator(random, inputParameterCount, operators, nandIndexMoveConstraints, moveConstraintToMove, operatorsUsed);
    }

    private Span<NandIndexMoveConstraint> GetMoveConstraintsOfAllUsedNands(int inputParameterCount, int outputCount, ReadOnlySpan<NandOperator> nandOperators, FastResetBoolArray operatorsUsed)
    {
        var nandMoveConstraints = _nandMoveConstraints;
        Array.Fill(nandMoveConstraints, new NandMoveConstraint(int.MinValue, int.MaxValue));
        int nodesUsedCount = 0;
        for (int i = 0; i < nandOperators.Length; i++)
        {
            if (!operatorsUsed[i])
            {
                continue;
            }

            nodesUsedCount++;
            AddIndexesToStack(inputParameterCount, nandOperators[i], nandMoveConstraints);

            NandOperator nandOperator = nandOperators[i];
            // Calculation goes from left to right so can't move left of operator  value it uses
            nandMoveConstraints[i + inputParameterCount].MaxExclusiveLowerBound = Math.Max(nandOperator.LeftValueIndex, nandOperator.RightValueIndex);
        }

        // Output nands can not be moved
        Span<NandIndexMoveConstraint> nandsUsedMoveConstraints = _nandsUsedMoveConstraints.AsSpan(0, nodesUsedCount - outputCount);
        int nodeIndexesUsedFreeIndex = 0;
        for (int i = 0; i < nandMoveConstraints.Length - outputCount; i++)
        {
            if (i >= inputParameterCount && operatorsUsed[i - inputParameterCount])
            {
                nandsUsedMoveConstraints[nodeIndexesUsedFreeIndex++] = new NandIndexMoveConstraint(i, nandMoveConstraints[i]);
            }
        }

        return nandsUsedMoveConstraints;
    }

    private static void AddIndexesToStack(int inputParameterCount, NandOperator nandOperator, NandMoveConstraint[] nandMoveConstraints)
    {
        int leftIndex = nandOperator.LeftValueIndex - inputParameterCount;
        if (leftIndex >= 0)
        {
            AddOrUpdateMovConstraint(inputParameterCount, leftIndex, nandMoveConstraints);
        }

        int rightIndex = nandOperator.RightValueIndex - inputParameterCount;
        if (rightIndex >= 0)
        {
            AddOrUpdateMovConstraint(inputParameterCount, rightIndex, nandMoveConstraints);
        }
    }

    private static void AddOrUpdateMovConstraint(int inputParameterCount, int nandOperatorIndex, NandMoveConstraint[] nandMoveConstraints)
    {
        // Calculation goes from left to right so operator can never move beyond any operator that uses it
        nandMoveConstraints[nandOperatorIndex + inputParameterCount].MinExclusiveUpperBound = Math.Min(nandMoveConstraints[nandOperatorIndex + inputParameterCount].MinExclusiveUpperBound, nandOperatorIndex + inputParameterCount);
    }

    private static void TryMoveOperator(Random random,
                                        int inputParameterCount,
                                        Span<NandOperator> operators,
                                        Span<NandIndexMoveConstraint> nandIndexMoveConstraints,
                                        int moveConstraintToMove,
                                        FastResetBoolArray operatorsUsed)
    {
        NandIndexMoveConstraint moveConstraint = nandIndexMoveConstraints[moveConstraintToMove];
        int actualMinMoveIndex = moveConstraint.MoveConstraint.MaxExclusiveLowerBound + 1;
        // Not allowed to move into parameters as that is not operator space
        actualMinMoveIndex = Math.Max(actualMinMoveIndex, inputParameterCount);

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
            if (operatorsUsed[i - inputParameterCount])
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
            if (!operatorsUsed[i - inputParameterCount])
            {
                if (moveableIndex-- == 0)
                {
                    moveTo = i;
                    operators[moveTo - inputParameterCount] = operators[moveFrom - inputParameterCount];

                    operatorsUsed[moveFrom - inputParameterCount] = false;
                    operatorsUsed[moveTo - inputParameterCount] = true;
                    break;
                }
            }
        }

        Debug.Assert(moveTo != -1, "Logic for moveable space is invalid. The expected amount of available space was not found.");
        Debug.Assert(moveTo < operators.Length + inputParameterCount);

        // Need to update all operators that points to the move operator
        // so they now use the operators new index
        for (int i = actualMaxMoveIndex - inputParameterCount; i < operators.Length; i++)
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
}
