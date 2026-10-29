using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Api;

public record PlaceOrderRequest(
    [Required, StringLength(32)] string Sku,
    [Range(1, 100)] int Quantity);

public record OrderResponse(Guid Id, string Sku, int Quantity, DateTimeOffset PlacedAt);

public interface IOrderService
{
    Task<OrderResponse> PlaceAsync(string customerId, PlaceOrderRequest request, CancellationToken ct);
    Task<OrderResponse?> FindAsync(string customerId, Guid id, CancellationToken ct);
}

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Place(PlaceOrderRequest request, CancellationToken ct)
    {
        var order = await orders.PlaceAsync(CustomerId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken ct)
    {
        var order = await orders.FindAsync(CustomerId(), id, ct);
        return order is null ? NotFound() : order;
    }

    private string CustomerId() =>
        User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("The signed-in user has no subject claim");
}
