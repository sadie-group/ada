namespace Ada.Core.Shared.Constants;

public static class PurchaseLimits
{
    public const int MaxPurchaseAmount = 100;

    public static bool IsValidAmount(int amount) => amount is >= 1 and <= MaxPurchaseAmount;
}
