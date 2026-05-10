using Equation.Solver.DataSources.Formats.SomeDatasetFormat;
using System.Text.Json.Serialization;

namespace Equation.Solver;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SomeDatasetJsonFormat))]
internal partial class SomeDatasetJsonFormatContext : JsonSerializerContext { }
