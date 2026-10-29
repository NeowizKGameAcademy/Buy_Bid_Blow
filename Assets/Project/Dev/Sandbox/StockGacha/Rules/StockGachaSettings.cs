using System;
namespace BuyBidBlow.Systems.StockGacha
{
    [Serializable]
    public sealed class StockGachaSettings
    {
        public int Price = 10;
        public int Blanks = 70;
        public int SharesPerCompany = 10;
        public void Validate()
        {
            if(Price<=0 || Blanks<0 || SharesPerCompany<0 ||
               (long)Blanks+3L*SharesPerCompany<=0 || (long)Blanks+3L*SharesPerCompany>int.MaxValue)
                throw new ArgumentException("Invalid stock gacha settings.");
        }
    }
}
