using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Abstractions.Services;

public interface ICartService
{
    Task<CartSummaryDto> BuildCartSummaryAsync(IReadOnlyList<CartLineQuantityDto> lines, CancellationToken cancellationToken = default);
}

