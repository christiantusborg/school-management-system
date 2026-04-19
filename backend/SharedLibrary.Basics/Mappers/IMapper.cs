namespace QuVian.SharedLibrary.Basics.Mappers;

/// <summary>
///     Mapper interface for mapping between two values.
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TOutput"></typeparam>
public interface IMapper<in TInput, out TOutput>
{
    /// <summary>
    ///     maps between the input and output values.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    TOutput MapFrom(TInput input);
}
