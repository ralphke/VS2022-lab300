using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Store.Services;

public class CartCircuitHandler : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Circuit opened - scoped CartService will be created when first resolved
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Circuit closed - scoped CartService will be disposed by DI container
        // This ensures cart state is cleaned up when the circuit ends
        return Task.CompletedTask;
    }
}
