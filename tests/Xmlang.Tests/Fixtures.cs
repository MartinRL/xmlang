using Emlang;

namespace Xmlang.Tests;

/// <summary>Shared synthetic Event Model (emlang) fixture. A tiny two-slice shop:
/// covers initiator roles, short element keys, an `s:` decision model with a phase enum, a
/// quoted "Todo / ..." view, a Screen-lane view, and a complex-typed field for self paths.</summary>
internal static class Fixtures
{
    public const string Em = """
        slices:
          "🧑 OpenShop":
            - a: owner / Catalog
            - c: OpenShop
              props:
                shopId: Guid
                name: string
            - e: Shop / ShopOpened
              props:
                shopId: Guid
            - s: Shop
              props:
                phase: ShopPhase (closed|open)
                shopId: Guid
          "🛒 BrowseShop":
            - t: customer / Storefront
            - c: AddItem
              props:
                itemId: Guid
                quantity: int
            - x: ShopClosed
            - e: Shop / ItemAdded
              props:
                itemId: Guid
            - v: Storefront
              props:
                items: Item[]
                total: decimal
                note: string
            - v: "Todo / Outstanding bids"
              props:
                bids: Bid[]
            - v: Screen / Results
              props:
                summary: string
        """;

    public static EmSpec ParsedEm => EmParser.Parse(Em);
}
