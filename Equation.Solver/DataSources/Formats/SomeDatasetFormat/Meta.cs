namespace Equation.Solver.DataSources.Formats.SomeDatasetFormat;

internal sealed class Meta
{
    public required int input_tokens { get; set; }
    public required int output_tokens { get; set; }
    public required string teacher_model { get; set; }
}
