using Equation.Solver.Solvers;
using System.Runtime.Intrinsics;

namespace Equation.Solver.Tests.Utilities;

internal static class EquationTools
{
    public static ProblemParts CreateRandomEquation(int seed) => CreateRandomEquation(new Random(seed));

    public static ProblemParts CreateRandomEquation(Random random)
    {
        const int minParameterCount = 1;
        const int maxParameterCount = 10;
        const int minOutputCount = 1;
        const int maxOutputCount = 10;
        const int minExampleCount = 1;
        const int maxExampleCount = 1000;
        const int minOperatorCount = 10;
        const int maxOperatorCount = 100;

        int parameterCount = random.Next(minParameterCount, maxParameterCount + 1);
        int outputCount = random.Next(minOutputCount, maxOutputCount + 1);
        int exampleCount = random.Next(minExampleCount, maxExampleCount + 1);
        int operatorCount = random.Next(minOperatorCount, maxOperatorCount + 1);

        (bool[] Inputs, bool[] Outputs)[] examples = new (bool[], bool[])[exampleCount];
        for (int i = 0; i < examples.Length; i++)
        {
            // Don't care about duplicate inputs with different outputs.
            // This is because this problem does not aim to have a perfect solution.
            // The goal of this is only to have a valid problem, not a solveable one.
            examples[i].Inputs = random.GetItems([true, false], parameterCount);
            examples[i].Outputs = random.GetItems([true, false], outputCount);
        }

        ProblemParts problemParts = CreateUnsetEquationWithExamples(examples, operatorCount);
        RandomSolver.Randomize(random, problemParts.Equation, problemParts.EquationValues);

        problemParts.Equation.RecalculateOperatorsUsed(problemParts.EquationValues.StaticResultSize);
        return problemParts;
    }

    public static ProblemParts CreateEquationWithExamples((bool[], bool[])[] examples, NandOperator[] operators)
    {
        ProblemParts problemParts = CreateUnsetEquationWithExamples(examples, operators.Length);
        operators.CopyTo(problemParts.Equation.NandOperators);
        problemParts.Equation.RecalculateOperatorsUsed(problemParts.EquationValues.StaticResultSize);

        return problemParts;
    }

    public static ProblemParts CreateUnsetEquationWithExamples((bool[], bool[])[] problemExamples, int operatorCount)
    {
        var examples = ProblemExample.ConvertToExamples(problemExamples).ToArray();
        var equationValues = new EquationValues(examples[0].Input.Count, operatorCount);
        var equation = new ProblemEquation(operatorCount, examples[0].Output.Count);

        return new ProblemParts(equation, equationValues, examples, new EquationProblem(examples));
    }
}
