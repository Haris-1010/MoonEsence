using System.Text.Json;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.Helpers;

public static class CartSessionStore
{
    private const string CartKey = "cart.lines.v1";

    public static List<CartLineQuantityDto> GetCartLines(ISession session)
    {
        var json = session.GetString(CartKey);
        if (string.IsNullOrWhiteSpace(json))
            return new List<CartLineQuantityDto>();

        try
        {
            var lines = JsonSerializer.Deserialize<List<CartLineQuantityDto>>(json);
            return lines ?? new List<CartLineQuantityDto>();
        }
        catch
        {
            return new List<CartLineQuantityDto>();
        }
    }

    public static void SetCartLines(ISession session, List<CartLineQuantityDto> lines)
    {
        var json = JsonSerializer.Serialize(lines);
        session.SetString(CartKey, json);
    }

    public static void ClearCartLines(ISession session) => session.Remove(CartKey);
}

