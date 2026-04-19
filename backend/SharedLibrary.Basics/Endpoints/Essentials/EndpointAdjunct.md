# Using `EndpointAdjunct` as a Base Class

## When and Why to Use `EndpointAdjunct`

### 1. Consistent Endpoint Behavior

#### Standardization

- Provides a standardized way of defining endpoint behavior.
- Ensures that all endpoints adhere to a consistent implementation pattern.

#### Common Functionality

- Ideal for implementing common functionalities across multiple endpoints.
- Useful for logging, error handling, or response formatting.

### 2. Overridable Configuration

#### Flexibility in Endpoint Definitions

- Allows customizing endpoint attributes like URI, name, tag, summary, and description.
- Maintains a base level of standardization.

#### Dynamic Behavior

- Methods check for overridden information before fetching default values.
- Allows dynamic behavior changes in endpoint characteristics.

### 3. Improved Code Organization and Maintenance

#### Cleaner Code

- Organizes code better and prevents duplication.
- Leads to cleaner, more maintainable code.

#### Easier Updates and Maintenance

- Changes in common functionalities require updates only in `EndpointAdjunct`.
- Simplifies maintenance across all derived endpoint classes.

### 4. Enhanced Readability and Documentation

#### Self-documenting Code

- Methods like `GetEndpointSummary` and `GetEndpointDescription` enhance readability.
- Serve as self-documenting features for understanding endpoint purposes.

#### Ease of Documentation

- These methods can be directly used to generate API documentation.
- Provides clear and consistent descriptions for endpoint functionalities.

### 5. Decoupling and Flexibility

#### Decoupling

- Acts as a decoupling layer between endpoint implementation and underlying logic.
- Promotes separation of concerns.

#### Extension Points

- Provides extension points for future enhancements.
- Useful for adding new methods for additional common functionalities.

## Methods

### Overwrite with custom implementation

All result can be overwritten with custom implementation, by adding a class that implements `IEndpointAdjunctOverwrite`
interface.
Based on the action, the class name must be `DemoV1<Action>Endpoint<Method>`, where:

Action can be:

- Create
- Get
- GetAll
- Update
- Delete
- Undelete

Method can be:

- Uri
- Name
- Tag
- Summary
- Description

### Usage example

The `EndpointAdjunct` can be used to extend the default `app.MapGet` interface.

```csharp
    app.MapGet<ExampleV1CreateCommand, ExampleV1CreateEndpointResponse>(this, EndpointHandler);
```
Complete example of the `EndpointAdjunct` class:
```csharp
using ezflux.Example.Application.V1.Create.Command;

namespace ezflux.Example.Application.V1.Create.Endpoint;

/// <summary>
///     Represents the endpoint for creating Example in the system.
/// </summary>
public sealed class ExampleV1CreateEndpoint : IEndpointMarker
{

    /// <inheritdoc cref="ExampleV1CreateEndpoint" />
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        //v1/Example
        app.MapPost<ExampleV1CreateCommand, ExampleV1CreateEndpointResponse>(this, EndpointHandler);
        return app;
    }

    /// <inheritdoc cref="ExampleV1CreateEndpoint" />
    private async Task<IResult> EndpointHandler([FromBody] ExampleV1CreateEndpointRequest request,
        ISender sender,
        IMapper<ExampleV1CreateEndpointRequest, ExampleV1CreateCommand> requestMapper,
        IMapper<ExampleV1CreateCommandResult, ExampleV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);

        var commandResult = await sender.Send(command, cancellationToken).ConfigureAwait(false);

        var result = commandResult.ToCreatedResult(responseMapper);
        return result;
    }
}

```


