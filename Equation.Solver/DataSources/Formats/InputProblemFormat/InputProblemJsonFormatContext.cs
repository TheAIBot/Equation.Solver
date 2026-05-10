using Equation.Solver.DataSources.Formats.InputProblemFormat;
using System.Text.Json.Serialization;

namespace Equation.Solver;

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(InputProblemJsonFormat))]
internal partial class InputProblemJsonFormatContext : JsonSerializerContext { }
