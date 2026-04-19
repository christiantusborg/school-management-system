namespace QuVian.SharedLibrary.Basics.DataClassifications;

[AttributeUsage(AttributeTargets.Property)]
public class DataClassificationAttribute : Attribute
{
    public ClassificationLevels Level { get; }
    public string Reason { get; }

    public DataClassificationAttribute(ClassificationLevels level, string reason)
    {
        Level = level;
        Reason = reason;
    }
}