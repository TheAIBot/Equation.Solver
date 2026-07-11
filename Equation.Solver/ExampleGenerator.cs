using System.Text;

namespace Equation.Solver;

internal readonly record struct ExampleGenerator
{
    private const int _bitsPerByte = 8;
    private const int _maxBytesPerRune = 4;
    private readonly Rune[] _input;
    private readonly int _inputUtf8ByteLength;
    private readonly Rune[] _output;
    private readonly int[] _outputPrefixLengths;

    public int MaxInputLength => (_inputUtf8ByteLength + CountUtf8Bytes(_output.AsSpan(0, _outputPrefixLengths.Max()))) * _bitsPerByte;
    public int MaxOutputLength => _maxBytesPerRune * _bitsPerByte;

    private ExampleGenerator(Rune[] input,
                             int inputUtf8ByteLength,
                             Rune[] output,
                             int[] outputPrefixLengths)
    {
        _input = input;
        _inputUtf8ByteLength = inputUtf8ByteLength;
        _output = output;
        _outputPrefixLengths = outputPrefixLengths;
    }

    public static ExampleGenerator CreateRandom(Random random, HashSet<int> usedIndexes, string input, string output, int examplesPerProblem)
    {
        Rune[] inputRunes = input.EnumerateRunes().ToArray();
        Rune[] outputRunes = output.EnumerateRunes().ToArray();

        usedIndexes.Clear();
        int maxExampleCount = Math.Min(examplesPerProblem, outputRunes.Length);
        int exampleCount = 0;
        int[] prefixLengths = new int[examplesPerProblem];
        while (exampleCount < maxExampleCount)
        {
            int prefixLength = random.Next(0, outputRunes.Length);
            if (!usedIndexes.Add(prefixLength))
            {
                continue;
            }

            prefixLengths[exampleCount] = prefixLength;
            exampleCount++;
        }

        // If output isn't large enough for the number of duplicates then
        // the last examples are just duplicated to fill it out.
        // This simplifies later code since all generators has the same length.
        while (exampleCount + 1 < prefixLengths.Length)
        {
            prefixLengths[exampleCount + 1] = prefixLengths[exampleCount];
            exampleCount++;
        }

        Array.Sort(prefixLengths);
        return new ExampleGenerator(inputRunes, CountUtf8Bytes(inputRunes), outputRunes, prefixLengths);
    }

    public bool[] GetInput(int index)
    {
        int outputRuneCount = _outputPrefixLengths[index];
        ReadOnlySpan<Rune> outputPart = _output.AsSpan(0, outputRuneCount);
        int byteCount = _inputUtf8ByteLength + CountUtf8Bytes(outputPart);
        byte[] allUtf8Bytes = new byte[byteCount];
        Span<byte> utf8Bytes = allUtf8Bytes;

        for (int i = 0; i < _input.Length; i++)
        {
            int writtenByteCount = _input[i].EncodeToUtf8(utf8Bytes);
            utf8Bytes = utf8Bytes.Slice(writtenByteCount);
        }

        for (int i = 0; i < outputPart.Length; i++)
        {
            int writtenByteCount = outputPart[i].EncodeToUtf8(utf8Bytes);
            utf8Bytes = utf8Bytes.Slice(writtenByteCount);
        }

        return Program.TextToBools(allUtf8Bytes);
    }

    public bool[] GetOutput(int index)
    {
        Span<byte> utf8Bytes = stackalloc byte[_maxBytesPerRune];
        int writtenByteCount = _output[_outputPrefixLengths[index]].EncodeToUtf8(utf8Bytes);
        utf8Bytes = utf8Bytes.Slice(0, writtenByteCount);

        return Program.TextToBools(utf8Bytes);
    }

    private static int CountUtf8Bytes(ReadOnlySpan<Rune> runes)
    {
        int byteCount = 0;
        for (int i = 0; i < runes.Length; i++)
        {
            byteCount += runes[i].Utf8SequenceLength;
        }

        return byteCount;
    }
}
