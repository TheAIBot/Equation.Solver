using Equation.Solver.Evolvers;
using Equation.Solver.Tests.Utilities;
using System.Runtime.Intrinsics;
using Xunit;

namespace Equation.Solver.FuzzyTests;

public sealed class MultiNandMoverTest
{
    public static TheoryData<int> TestData(int testCount)
    {
        var data = new TheoryData<int>();
        Random random = new Random(1);
        for (int i = 0; i < testCount; i++)
        {
            data.Add(random.Next());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(TestData), parameters: [10000])]
    public void MoveRandomNandOperators_Move_ExpectOperatorsUsedIsCorrectAndResultIsTheSame(int randomSeed)
    {
        Random random = new Random(randomSeed);
        int moveCount = random.Next(10, 100);
        ProblemParts problemParts = EquationTools.CreateRandomEquation(randomSeed);
        Vector256<int>[] expectedResults = problemParts.EquationProblem.GetEquationResults(problemParts.Equation, problemParts.EquationValues);
        MultiNandMover nandMover = new MultiNandMover(new NandMover(problemParts.EquationValues.InputParameterCount, problemParts.Equation.NandOperators.Length), moveCount);

        nandMover.MoveRandomNandOperators(random, problemParts.EquationValues.InputParameterCount, problemParts.Equation.OutputSize, problemParts.Equation.NandOperators, problemParts.Equation.OperatorsUsed);

        Vector256<int>[] actualResults = problemParts.EquationProblem.GetEquationResults(problemParts.Equation, problemParts.EquationValues);
        Assert.Equal(expectedResults, actualResults);

        bool[] usedOperatorsAfterMove = new bool[problemParts.Equation.NandOperators.Length];
        problemParts.Equation.OperatorsUsed.CopyTo(usedOperatorsAfterMove);

        problemParts.Equation.OperatorsUsed.Clear();
        problemParts.Equation.RecalculateOperatorsUsed(problemParts.EquationValues.InputParameterCount);
        bool[] recalculatedOperatorsUsedAfterMove = new bool[problemParts.Equation.NandOperators.Length];
        problemParts.Equation.OperatorsUsed.CopyTo(recalculatedOperatorsUsedAfterMove);

        Assert.Equal(usedOperatorsAfterMove, recalculatedOperatorsUsedAfterMove);
    }
}

public sealed class NandMoverTest
{
    public static TheoryData<int> TestData(int testCount)
    {
        var data = new TheoryData<int>();
        Random random = new Random(1);
        for (int i = 0; i < testCount; i++)
        {
            data.Add(random.Next());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(TestData), parameters: [10000])]
    public void MoveRandomNandOperator_Move_ExpectOperatorsUsedIsCorrectAndResultIsTheSame(int randomSeed)
    {
        Random random = new Random(randomSeed);
        ProblemParts problemParts = EquationTools.CreateRandomEquation(randomSeed);
        Vector256<int>[] expectedResults = problemParts.EquationProblem.GetEquationResults(problemParts.Equation, problemParts.EquationValues);
        NandMover nandMover = new NandMover(problemParts.EquationValues.InputParameterCount, problemParts.Equation.NandOperators.Length);

        nandMover.MoveRandomNandOperator(random, problemParts.EquationValues.InputParameterCount, problemParts.Equation.OutputSize, problemParts.Equation.NandOperators, problemParts.Equation.OperatorsUsed);

        Vector256<int>[] actualResults = problemParts.EquationProblem.GetEquationResults(problemParts.Equation, problemParts.EquationValues);
        Assert.Equal(expectedResults, actualResults);

        bool[] usedOperatorsAfterMove = new bool[problemParts.Equation.NandOperators.Length];
        problemParts.Equation.OperatorsUsed.CopyTo(usedOperatorsAfterMove);

        problemParts.Equation.OperatorsUsed.Clear();
        problemParts.Equation.RecalculateOperatorsUsed(problemParts.EquationValues.InputParameterCount);
        bool[] recalculatedOperatorsUsedAfterMove = new bool[problemParts.Equation.NandOperators.Length];
        problemParts.Equation.OperatorsUsed.CopyTo(recalculatedOperatorsUsedAfterMove);

        Assert.Equal(usedOperatorsAfterMove, recalculatedOperatorsUsedAfterMove);
    }
}
