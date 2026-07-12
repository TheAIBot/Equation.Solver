using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Equation.Solver.DataSources.JsonLines;

internal sealed class JsonLinesConverter
{
    public static IAsyncEnumerable<T> ReadJsonLines<T>(string filePath)
        where T : IJsonConverterType<T>
    {
        JsonTypeInfo<T> jsonTypeInfo = T.GetJsonTypeInfo();
        return Parallel.Transform(File.ReadLines(filePath),
                                  line => JsonSerializer.Deserialize(line, jsonTypeInfo) ?? throw new InvalidOperationException($"Failed to deserialize json: \"{line}\""));
    }

    public static IAsyncEnumerable<TOutput> ReadJsonLines<TInput, TOutput>(string inputFilePath, Func<TInput, TOutput> converter)
        where TInput : IJsonConverterType<TInput>
        where TOutput : IJsonConverterType<TOutput>
    {
        return Parallel.Transform(ReadJsonLines<TInput>(inputFilePath),
                                  converter);
    }


    public static async Task ConvertFile<TInput, TOutput>(string inputFilePath, string outputFilePath, Func<TInput, TOutput> converter)
        where TInput : IJsonConverterType<TInput>
        where TOutput : IJsonConverterType<TOutput>
    {
        JsonTypeInfo<TOutput> outputJsonTypeInfo = TOutput.GetJsonTypeInfo();
        byte[] newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);

        await using FileStream lineWriter = new FileStream(outputFilePath, new FileStreamOptions() { Mode = FileMode.Create, Access = FileAccess.Write, Options = FileOptions.Asynchronous });
        await foreach (TInput input in ReadJsonLines<TInput>(inputFilePath))
        {
            TOutput output = converter(input);
            await JsonSerializer.SerializeAsync(lineWriter, output, outputJsonTypeInfo);
            await lineWriter.WriteAsync(newLineBytes);
        }
    }
}
