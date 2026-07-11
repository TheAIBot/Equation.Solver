using Equation.Solver.Score;

namespace Equation.Solver;

internal readonly record struct EquationScore(int WrongBits, int MaxSequentialNandGates, int NandCount)
{
    public static readonly EquationScore MaxScore = new EquationScore(int.MaxValue, int.MaxValue, int.MaxValue);

    public SlimEquationScore ToSlimScore() => new SlimEquationScore(WrongBits);

    public override string ToString()
    {
        return $"{nameof(WrongBits)} = {WrongBits:N0}, {nameof(MaxSequentialNandGates)} = {MaxSequentialNandGates:N0}, {nameof(NandCount)} = {NandCount:N0}";
    }
}
