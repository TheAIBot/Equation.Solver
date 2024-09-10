using Equation.Solver.Tests.Utilities;
using System.Runtime.Intrinsics;

namespace Equation.Solver.UnitTests;

public sealed class ProblemEquationTest
{
    [Fact]
    public void Calculate_WithBitwiseNot_ExpectBitwiseNotResult()
    {
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false], [true]),
            ([true], [false]),
        ],
        [
            new NandOperator(0, 0)
        ]);

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples[0].Output.Count);
        Assert.Equal(problemParts.Examples[0].Output.Outputs[0].GetElement(0) & 0b11, actualResult[0].GetElement(0) & 0b11);
    }

    [Fact]
    public void Calculate_WithBitwiseAnd_ExpectBitwiseAndResult()
    {
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false]),
            ([false,  true], [false]),
            ([true, false], [false]),
            ([true,  true], [true]),
        ],
        [
            new NandOperator(0, 1),
            new NandOperator(2, 2)
        ]);

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples[0].Output.Count);
        Assert.Equal(problemParts.Examples[0].Output.Outputs[0].GetElement(0) & 0b1111, actualResult[0].GetElement(0) & 0b1111);
    }

    [Fact]
    public void Calculate_WithBitwiseOr_ExpectBitwiseOrResult()
    {
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false]),
            ([false,  true], [true]),
            ([true, false], [true]),
            ([true,  true], [true]),
        ],
        [
            new NandOperator(0, 0),
            new NandOperator(1, 1),
            new NandOperator(2, 3)
        ]);

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples[0].Output.Count);
        Assert.Equal(problemParts.Examples[0].Output.Outputs[0].GetElement(0) & 0b1111, actualResult[0].GetElement(0) & 0b1111);
    }

    [Fact]
    public void Calculate_WithBitwiseXor_ExpectBitwiseXorResult()
    {
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false]),
            ([false,  true], [true]),
            ([true, false], [true]),
            ([true,  true], [false]),
        ],
        [
            new NandOperator(0, 0),
            new NandOperator(1, 1),
            new NandOperator(0, 1),
            new NandOperator(2, 3),
            new NandOperator(4, 5),
            new NandOperator(6, 6),
        ]);

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples[0].Output.Count);
        Assert.Equal(problemParts.Examples[0].Output.Outputs[0].GetElement(0) & 0b1111, actualResult[0].GetElement(0) & 0b1111);
    }

    [Fact]
    public void Calculate_WithHalfAdderExamples_ExpectHalfAdderResult()
    {
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false, false]),
            ([false, true], [false, true]),
            ([true , false], [false, true]),
            ([true , true], [true , false]),
        ],
        [
            new NandOperator(0, 0),
            new NandOperator(1, 1),
            new NandOperator(0, 1),
            new NandOperator(2, 3),
            new NandOperator(4, 5),
            new NandOperator(4, 4),
            new NandOperator(6, 6),

        ]);

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples[0].Output.Count);
        Assert.Equal(problemParts.Examples[0].Output.Outputs[0].GetElement(0) & 0b1111, actualResult[0].GetElement(0) & 0b1111);
        Assert.Equal(problemParts.Examples[0].Output.Outputs[1].GetElement(0) & 0b1111, actualResult[1].GetElement(0) & 0b1111);
    }
}