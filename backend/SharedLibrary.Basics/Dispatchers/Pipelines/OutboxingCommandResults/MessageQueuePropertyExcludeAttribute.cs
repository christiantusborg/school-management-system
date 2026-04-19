namespace QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.OutboxingCommandResults;

/// <summary>
///     Class PermissionExcludeAttribute. This class cannot be inherited.
///     Implements the <see cref="System.Attribute" />.
/// </summary>
/// <seealso cref="System.Attribute" />
[AttributeUsage(AttributeTargets.Property)]
public sealed class MessageQueuePropertyExcludeAttribute : Attribute
{
}