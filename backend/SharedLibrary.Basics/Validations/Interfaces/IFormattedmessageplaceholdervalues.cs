namespace QuVian.SharedLibrary.Validations.Interfaces;

public interface IFormattedmessageplaceholdervalues
{
    /// <summary>
    /// Gets or sets the value of a placeholder in a formatted message.
    /// </summary>
    string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the value associated with a formatted message's placeholder.
    /// </summary>
    object PropertyValue { get; set; }
}