using Equation.Solver.DataSources.JsonLines;
using System.Text.Json.Serialization.Metadata;

namespace Equation.Solver.DataSources.Formats.SomeDatasetFormat;

internal sealed class SomeDatasetJsonFormat : IJsonConverterType<SomeDatasetJsonFormat>
{
    public required string Id { get; set; }
    public required Conversation[] Conversations { get; set; }
    public required string Input { get; set; }
    public required string Output { get; set; }
    public required string Domain { get; set; }
    public required Meta Meta { get; set; }

    public static JsonTypeInfo<SomeDatasetJsonFormat> GetJsonTypeInfo() => SomeDatasetJsonFormatContext.Default.SomeDatasetJsonFormat;
}
