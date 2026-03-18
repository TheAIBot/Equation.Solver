using Equation.Solver.Evolvers;
using Equation.Solver.Tests.Utilities;
using Xunit;

namespace Equation.Solver.FuzzyTests;

public sealed class NandChangerTest
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
    public void RandomizeSmallPartOfEquation_RandomEquations_ExpectNoExceptionsThrownAndOperatorsUsedIsCorrect(int randomSeed)
    {
        Random random = new Random(randomSeed);
        ProblemParts problemParts = EquationTools.CreateRandomEquation(randomSeed);
        NandChanger nandChanger = new NandChanger();
        int operatorChangeCount = random.Next(problemParts.Equation.NandOperators.Length + 1);

        nandChanger.RandomizeSmallPartOfEquation(random, problemParts.Equation, problemParts.EquationValues, operatorChangeCount);

        bool[] usedOperatorsAfterChange = new bool[problemParts.Equation.NandOperators.Length];
        problemParts.Equation.OperatorsUsed.CopyTo(usedOperatorsAfterChange);

        problemParts.Equation.OperatorsUsed.Clear();
        problemParts.Equation.RecalculateOperatorsUsed(problemParts.EquationValues.InputParameterCount);
        bool[] recalculatedOperatorsUsedAfterChange = new bool[problemParts.Equation.NandOperators.Length];
        problemParts.Equation.OperatorsUsed.CopyTo(recalculatedOperatorsUsedAfterChange);

        Assert.Equal(usedOperatorsAfterChange, recalculatedOperatorsUsedAfterChange);
    }
}