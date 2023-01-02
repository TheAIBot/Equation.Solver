namespace Equation.Solver;

internal readonly record struct EquationScore(int WrongBits, int MaxSequentialNandGates, int NandCount)
{
    public static readonly EquationScore MaxScore = new EquationScore(int.MaxValue, int.MaxValue, int.MaxValue);

    public SlimEquationScore ToSlimScore() => new SlimEquationScore(WrongBits);
}
