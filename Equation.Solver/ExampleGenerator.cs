namespace Equation.Solver;

internal readonly record struct ExampleGenerator
{
    private readonly string _Input;
    private readonly string _Output;
    private readonly int[] _OutputPrefixLengths;

    public int MaxInputLength => Program.TextToBools(_Input + _Output[.._OutputPrefixLengths.Max()]).Length;
    public int MaxOutputLength => Program.TextToBools(_Output[_OutputPrefixLengths.Max()].ToString()).Length;

    public ExampleGenerator(string input, string output, int[] outputPrefixLengths)
    {
        _Input = input;
        _Output = output;
        _OutputPrefixLengths = outputPrefixLengths;
    }

    public bool[] GetInput(int index)
    {
        return Program.TextToBools(_Input + _Output[.._OutputPrefixLengths[index]]);
    }

    public bool[] GetOutput(int index)
    {
        return Program.TextToBools(_Output[_OutputPrefixLengths[index]].ToString());
    }
}
