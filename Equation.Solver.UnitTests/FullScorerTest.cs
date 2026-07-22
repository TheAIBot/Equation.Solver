using Equation.Solver.Score;
using Equation.Solver.Tests.Utilities;

namespace Equation.Solver.UnitTests;

public sealed class FullScorerTest
{
    [Fact]
    public void ToFullScore_WithSingleNandGate_ExpectDepthOneAndNoIntermediateGates()
    {
        var scorer = new FullScorer(1);
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false], [true]),
            ([true], [false]),
        ],
        [
            new NandOperator(0, 0)
        ]);

        EquationScore score = scorer.ToFullScore(new SlimEquationScore(0), problemParts.EquationValues, problemParts.Equation);

        Assert.Equal(1, score.MaxSequentialNandGates);
        Assert.Equal(1, score.NandCount);
    }

    [Fact]
    public void ToFullScore_WithAndGate_ExpectDepthTwoAndOneIntermediateGate()
    {
        var scorer = new FullScorer(2);
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false]),
            ([false, true], [false]),
            ([true, false], [false]),
            ([true, true], [true]),
        ],
        [
            new NandOperator(0, 1),
            new NandOperator(2, 2)
        ]);

        EquationScore score = scorer.ToFullScore(new SlimEquationScore(0), problemParts.EquationValues, problemParts.Equation);

        Assert.Equal(2, score.MaxSequentialNandGates);
        Assert.Equal(2, score.NandCount);
    }

    [Fact]
    public void ToFullScore_WithOrGate_ExpectDepthTwoAndTwoIntermediateGates()
    {
        var scorer = new FullScorer(3);
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false]),
            ([false, true], [true]),
            ([true, false], [true]),
            ([true, true], [true]),
        ],
        [
            new NandOperator(0, 0),
            new NandOperator(1, 1),
            new NandOperator(2, 3)
        ]);

        EquationScore score = scorer.ToFullScore(new SlimEquationScore(0), problemParts.EquationValues, problemParts.Equation);

        Assert.Equal(2, score.MaxSequentialNandGates);
        Assert.Equal(3, score.NandCount);
    }

    [Fact]
    public void ToFullScore_WithChainOfThreeGates_ExpectDepthThreeAndTwoIntermediateGates()
    {
        var scorer = new FullScorer(3);
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false], [true]),
            ([true], [false]),
        ],
        [
            new NandOperator(0, 0),
            new NandOperator(1, 1),
            new NandOperator(2, 2)
        ]);

        EquationScore score = scorer.ToFullScore(new SlimEquationScore(0), problemParts.EquationValues, problemParts.Equation);

        Assert.Equal(3, score.MaxSequentialNandGates);
        Assert.Equal(3, score.NandCount);
    }

    [Fact]
    public void ToFullScore_WithTwoIndependentOutputs_ExpectDepthOneAndNoIntermediateGates()
    {
        var scorer = new FullScorer(2);
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [true, true]),
            ([false, true], [true, false]),
            ([true, false], [false, true]),
            ([true, true], [false, false]),
        ],
        [
            new NandOperator(0, 0),
            new NandOperator(1, 1)
        ]);

        EquationScore score = scorer.ToFullScore(new SlimEquationScore(0), problemParts.EquationValues, problemParts.Equation);

        Assert.Equal(1, score.MaxSequentialNandGates);
        Assert.Equal(2, score.NandCount);
    }

    [Fact]
    public void ToFullScore_WithXorGate_ExpectDepthFourAndFiveIntermediateGates()
    {
        var scorer = new FullScorer(6);
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false]),
            ([false, true], [true]),
            ([true, false], [true]),
            ([true, true], [false]),
        ],
        [
            new NandOperator(0, 0), // 2
            new NandOperator(1, 1), // 3
            new NandOperator(0, 1), // 4
            new NandOperator(2, 3), // 5
            new NandOperator(4, 5), // 6
            new NandOperator(6, 6),
        ]);

        EquationScore score = scorer.ToFullScore(new SlimEquationScore(0), problemParts.EquationValues, problemParts.Equation);

        Assert.Equal(4, score.MaxSequentialNandGates);
        Assert.Equal(6, score.NandCount);
    }

    [Fact]
    public void ToFullScore_WithHalfAdder_ExpectDepthFourAndFiveIntermediateGates()
    {
        var scorer = new FullScorer(7);
        ProblemParts problemParts = EquationTools.CreateEquationWithExamples(
        [
            ([false, false], [false, false]),
            ([false, true], [false, true]),
            ([true, false], [false, true]),
            ([true, true], [true, false]),
        ],
        [
            new NandOperator(0, 0), // 2
            new NandOperator(1, 1), // 3
            new NandOperator(0, 1), // 4
            new NandOperator(2, 3), // 5
            new NandOperator(4, 5), // 6
            new NandOperator(4, 4), // 7
            new NandOperator(6, 6),
        ]);

        EquationScore score = scorer.ToFullScore(new SlimEquationScore(0), problemParts.EquationValues, problemParts.Equation);

        Assert.Equal(4, score.MaxSequentialNandGates);
        Assert.Equal(7, score.NandCount);
    }
}
