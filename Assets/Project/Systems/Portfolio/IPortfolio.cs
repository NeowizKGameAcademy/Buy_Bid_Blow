using BuyBidBlow.Systems.Types;

namespace BuyBidBlow
{
    public interface IPortfolio
    {
        int GetOwnedQuantity(Company company);

        void AddStock(Company company, int quantity);
        bool TryRemoveStock(Company company, int quantity);
    }
}
