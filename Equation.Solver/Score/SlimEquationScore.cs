namespace Equation.Solver.Score;

internal readonly record struct SlimEquationScore(int WrongBits) : IComparable<SlimEquationScore>
{
    public int CompareTo(SlimEquationScore other)
    {
        return WrongBits.CompareTo(other.WrongBits);
    }

    public static bool operator <(SlimEquationScore left, SlimEquationScore right)
    {
        return left.WrongBits < right.WrongBits;
    }

    public static bool operator >(SlimEquationScore left, SlimEquationScore right)
    {
        return left.WrongBits > right.WrongBits;
    }

    public static bool operator <(SlimEquationScore left, EquationScore right)
    {
        return left.WrongBits < right.WrongBits;
    }

    public static bool operator >(SlimEquationScore left, EquationScore right)
    {
        return left.WrongBits > right.WrongBits;
    }
}
