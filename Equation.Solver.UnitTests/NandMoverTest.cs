using Equation.Solver.Evolvers;
using Equation.Solver.Tests.Utilities;

namespace Equation.Solver.UnitTests;

public sealed class NandMoverTest
{
    [Fact]
    public void MoveRandomNandOperator_WithFreeSpaceBetweenOperatorAndOutput_ExpectCanMoveToAllFreePositions()
    {
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false]),
        ],
        [
            new NandOperator(0, 0),
            new NandOperator(0, 0),
            new NandOperator(0, 1), // <-- Testing if this can move to all other unused spaces
            new NandOperator(0, 0),
            new NandOperator(0, 0),
            new NandOperator(4, 4), // <-- This is the output nand operator so it can't move
        ]);


        int inputParameterCount = problemParts.EquationValues.InputParameterCount;
        int operatorCount = problemParts.Equation.NandOperators.Length;
        int outputCount = problemParts.Equation.OutputSize;

        HashSet<int> expectedPositions = [0, 1, 3, 4];
        HashSet<int> visitedPositions = [];

        var nandMover = new NandMover(inputParameterCount, operatorCount);
        var random = new Random(42);
        ProblemEquation copy = problemParts.Equation.Copy();

        const int maxAttempts = 1000;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Don't modify the original equation since the starting state of all iterations should be the same
            copy.CopyFrom(problemParts.Equation);

            nandMover.MoveRandomNandOperator(random, inputParameterCount, outputCount, copy.NandOperators, copy.OperatorsUsed);

            // Last nand operator points to where the moved operator is
            int movedNandIndex = copy.NandOperators[^1].LeftValueIndex - inputParameterCount;
            visitedPositions.Add(movedNandIndex);

            if (visitedPositions.SetEquals(expectedPositions))
            {
                break;
            }
        }

        Assert.Equal(expectedPositions, visitedPositions);
    }
}
