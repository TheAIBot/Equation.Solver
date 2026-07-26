using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Equation.Solver;

internal readonly struct NandOperator
{
    private readonly uint _leftValueIndex;
    private readonly uint _rightValueIndex;

    public readonly int LeftValueIndex => (int)(_leftValueIndex / (uint)Vector256<int>.Count);
    public readonly int RightValueIndex => (int)(_rightValueIndex / (uint)Vector256<int>.Count);

    public NandOperator(int leftValueIndex, int rightValueIndex)
    {
        // Pre-multiplied by 8 since these are not an index into
        // a Vector256<int>[] but an int[]  where 8 elements
        // together are an Vector256<int>.
        _leftValueIndex = (uint)(leftValueIndex * Vector256<int>.Count);
        _rightValueIndex = (uint)(rightValueIndex * Vector256<int>.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void Nand(int* allValues, int* inputIndexes, int* vectors, int inputCount, int* storeLocation)
    {
        BatchNand(allValues, inputIndexes, vectors, inputCount, 1, storeLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void BatchNand(int* allValues, int* inputIndexes, int* vectors, int inputCount, int batchSize, int* storeLocation)
    {
        uint leftValueIndex = _leftValueIndex;
        uint rightValueIndex = _rightValueIndex;

        if (leftValueIndex < inputCount && rightValueIndex < inputCount)
        {
            leftValueIndex = leftValueIndex / (uint)Vector256<int>.Count;
            rightValueIndex = rightValueIndex / (uint)Vector256<int>.Count;
            int* leftValue = vectors;
            int* rightValue = vectors;
            for (int i = 0; i < batchSize; i++)
            {
                Vector256<int> opLeft = Vector256.LoadAligned(leftValue + inputIndexes[leftValueIndex * batchSize + i]);
                Vector256<int> opRight = Vector256.LoadAligned(rightValue + inputIndexes[rightValueIndex * batchSize + i]);
                Vector256.StoreAligned(~(opLeft & opRight), storeLocation + i * Vector256<int>.Count);
            }
        }
        else if (leftValueIndex < inputCount)
        {
            leftValueIndex = leftValueIndex / (uint)Vector256<int>.Count;
            int* leftValue = vectors;
            int* rightValue = allValues + rightValueIndex * batchSize;

            for (int i = 0; i < batchSize; i++)
            {
                Vector256<int> opLeft = Vector256.LoadAligned(leftValue + inputIndexes[leftValueIndex * batchSize + i]);
                Vector256<int> opRight = Vector256.LoadAligned(rightValue + i * Vector256<int>.Count);
                Vector256.StoreAligned(~(opLeft & opRight), storeLocation + i * Vector256<int>.Count);
            }
        }
        else if (rightValueIndex < inputCount)
        {
            rightValueIndex = rightValueIndex / (uint)Vector256<int>.Count;
            int* leftValue = allValues + leftValueIndex * batchSize;
            int* rightValue = vectors;

            for (int i = 0; i < batchSize; i++)
            {
                Vector256<int> opLeft = Vector256.LoadAligned(leftValue + i * Vector256<int>.Count);
                Vector256<int> opRight = Vector256.LoadAligned(rightValue + inputIndexes[rightValueIndex * batchSize + i]);
                Vector256.StoreAligned(~(opLeft & opRight), storeLocation + i * Vector256<int>.Count);
            }
        }
        else
        {
            int* leftValue = allValues + leftValueIndex * batchSize;
            int* rightValue = allValues + rightValueIndex * batchSize;

            for (int i = 0; i < batchSize; i++)
            {
                Vector256<int> opLeft = Vector256.LoadAligned(leftValue + i * Vector256<int>.Count);
                Vector256<int> opRight = Vector256.LoadAligned(rightValue + i * Vector256<int>.Count);
                Vector256.StoreAligned(~(opLeft & opRight), storeLocation + i * Vector256<int>.Count);
            }
        }
    }
}