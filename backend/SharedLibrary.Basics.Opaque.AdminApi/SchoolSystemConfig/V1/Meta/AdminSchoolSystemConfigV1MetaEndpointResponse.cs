namespace SharedLibrary.Basics.Opaque.AdminApi.SchoolSystemConfig.V1.Meta;

public sealed class AdminSchoolSystemConfigV1MetaEndpointResponse
{
    public required IList<EntityMetaDto> Entities { get; init; }
}

public sealed class EntityMetaDto
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string Endpoint { get; init; }
    public required string IdKey { get; init; }
    public required IList<ColumnMetaDto> Columns { get; init; }
    public required IList<FieldMetaDto> Fields { get; init; }
}

public sealed class ColumnMetaDto
{
    public required string Key { get; init; }
    public required string Label { get; init; }
}

public sealed class FieldMetaDto
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string Type { get; init; }
    public required bool Required { get; init; }
    public IList<SelectOptionDto>? Options { get; init; }
}

public sealed class SelectOptionDto
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}
