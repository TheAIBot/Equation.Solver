using System.Runtime.Intrinsics;

namespace Equation.Solver.UnitTests;

public sealed class ProblemEquationTest
{
    [Fact]
    public void Calculate_WithBitwiseAnd_ExpectBitwiseAndResult()
    {
        ProblemParts problemParts = CreateEquationWithExamples(new (bool[], bool[])[]
        {
            (new[] {false, false}, new[] { false}),
            (new[] {false,  true}, new[] { false}),
            (new[] { true, false}, new[] { false}),
            (new[] { true,  true}, new[] { true}),
        },
        new NandOperator[]
        {
            new NandOperator(0, 1),
            new NandOperator(2, 2)
        });

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples.Output.Count);
        Assert.Equal(problemParts.Examples.Output.Outputs[0], actualResult[0]);
    }

    [Fact]
    public void Calculate_WithBitwiseOr_ExpectBitwiseOrResult()
    {
        ProblemParts problemParts = CreateEquationWithExamples(new (bool[], bool[])[]
        {
            (new[] {false, false}, new[] { false}),
            (new[] {false,  true}, new[] { true }),
            (new[] { true, false}, new[] { true }),
            (new[] { true,  true}, new[] { true }),
        },
        new NandOperator[]
        {
            new NandOperator(0, 0),
            new NandOperator(1, 1),
            new NandOperator(2, 3)
        });

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples.Output.Count);
        Assert.Equal(problemParts.Examples.Output.Outputs[0], actualResult[0]);
    }

    [Fact]
    public void Calculate_WithBitwiseXor_ExpectBitwiseXorResult()
    {
        ProblemParts problemParts = CreateEquationWithExamples(new (bool[], bool[])[]
        {
            (new[] {false, false}, new[] { false}),
            (new[] {false,  true}, new[] { true }),
            (new[] { true, false}, new[] { true }),
            (new[] { true,  true}, new[] { false}),
        },
        new NandOperator[]
        {
            new NandOperator(0, 0),
            new NandOperator(1, 1),
            new NandOperator(0, 1),
            new NandOperator(2, 3),
            new NandOperator(4, 5),
            new NandOperator(6, 6),
        });

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples.Output.Count);
        Assert.Equal(problemParts.Examples.Output.Outputs[0], actualResult[0]);
    }

    [Fact]
    public void Calculate_WithHalfAdderExamples_ExpectHalfAdderResult()
    {
        ProblemParts problemParts = CreateEquationWithExamples(new (bool[], bool[])[]
        {
            (new[] {false, false}, new[] { false, false}),
            (new[] {false, true }, new[] { false, true }),
            (new[] {true , false}, new[] { false, true }),
            (new[] {true , true }, new[] { true , false}),
        },
        new NandOperator[]
        {
            new NandOperator(0, 0),
            new NandOperator(1, 1),
            new NandOperator(0, 1),
            new NandOperator(2, 3),
            new NandOperator(4, 5),
            new NandOperator(4, 4),
            new NandOperator(6, 6),

        });

        ReadOnlySpan<Vector256<int>> actualResult = problemParts.Equation.Calculate(problemParts.EquationValues);

        Assert.Equal(actualResult.Length, problemParts.Examples.Output.Count);
        Assert.Equal(problemParts.Examples.Output.Outputs[0], actualResult[0]);
    }

    private static ProblemParts CreateEquationWithExamples((bool[], bool[])[] biArgExamples, NandOperator[] operators)
    {
        var examples = ProblemExample.ConvertToExamples(biArgExamples).Single();
        var equationValues = new EquationValues(examples.Input.Count, operators.Length);
        equationValues.SetParameters(examples.Input);
        var equation = new ProblemEquation(operators.Length, examples.Output.Count);
        for (int i = 0; i < operators.Length; i++)
        {
            equation.NandOperators[i] = operators[i];
        }

        return new ProblemParts(equation, equationValues, examples);
    }

    private sealed record ProblemParts(ProblemEquation Equation, EquationValues EquationValues, ProblemExample Examples);
}
