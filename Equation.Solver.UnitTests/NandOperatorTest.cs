using System.Runtime.Intrinsics;

namespace Equation.Solver.UnitTests;

public sealed class NandOperatorTest
{
    [Fact]
    public unsafe void Nand_WithNandTruthTable_ExpectBitwiseNand()
    {
        Span<Vector256<int>> values = TestHelper.CreateAlignedSpan(2);
        values[0] = Vector256.Create(0b1010);
        values[1] = Vector256.Create(0b1100);
        var nand = new NandOperator(0, 1);
        int expected = 0b0111;

        fixed (Vector256<int>* valuesPtr = values)
        {
            Vector256<int> actual = nand.Nand((int*)valuesPtr);

            Assert.Equal(expected, actual.GetElement(0) & 0b1111);
        }
    }
}