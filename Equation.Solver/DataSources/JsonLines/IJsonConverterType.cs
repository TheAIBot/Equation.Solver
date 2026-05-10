using System.Text.Json.Serialization.Metadata;

namespace Equation.Solver.DataSources.JsonLines;

internal interface IJsonConverterType<T>
{
    static abstract JsonTypeInfo<T> GetJsonTypeInfo();
}
