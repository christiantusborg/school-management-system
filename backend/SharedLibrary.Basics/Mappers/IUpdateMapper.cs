namespace QuVian.SharedLibrary.Basics.Mappers;

/// <summary>
/// Mapping interface to inhance the output with value(s) from the input.
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TOutput"></typeparam>
public interface IUpdateMapper<in TInput, TOutput> where TOutput : class
{
    /// <summary>
    /// Maps one or more values from input to the referenced output.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    void MapTo(TInput input, ref TOutput output);
}
