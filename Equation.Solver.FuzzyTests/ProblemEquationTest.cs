using Equation.Solver.Tests.Utilities;
using System.Runtime.Intrinsics;
using Xunit;

namespace Equation.Solver.FuzzyTests;

public sealed class ProblemEquationTest
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
    public void RecalculateOperatorsUsed_RandomEquations_ExpectUnusedOperatorsWillNotChangeResult(int randomSeed)
    {
        Random random = new Random(randomSeed);
        ProblemParts problemParts = EquationTools.CreateRandomEquation(randomSeed);
        problemParts.Equation.OperatorsUsed.SetRangeTrue(0, problemParts.Equation.OperatorsUsed.Length, true);
        Vector256<int>[] expectedResults = problemParts.EquationProblem.GetEquationResults(problemParts.Equation, problemParts.EquationValues);

        problemParts.Equation.RecalculateOperatorsUsed(problemParts.EquationValues.InputParameterCount);

        Vector256<int>[] actualResults = problemParts.EquationProblem.GetEquationResults(problemParts.Equation, problemParts.EquationValues);
        Assert.Equal(expectedResults, actualResults);
    }
}