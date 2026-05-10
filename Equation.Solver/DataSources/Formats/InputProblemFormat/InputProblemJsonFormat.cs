using Equation.Solver.DataSources.JsonLines;
using System.Text.Json.Serialization.Metadata;

namespace Equation.Solver.DataSources.Formats.InputProblemFormat;

internal sealed class InputProblemJsonFormat : IJsonConverterType<InputProblemJsonFormat>
{
    public required string Input { get; set; }
    public required string Output { get; set; }

    public static JsonTypeInfo<InputProblemJsonFormat> GetJsonTypeInfo() => InputProblemJsonFormatContext.Default.InputProblemJsonFormat;
}
