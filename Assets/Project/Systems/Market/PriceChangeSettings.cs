using System;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Market
{
    [Serializable]
    public sealed class PriceChangeSettings
    {
        // Shared magnitude range; the generator picks the direction.
        public IntRange Amount;

        // Indexed by Company: Straw, Timber, Brick.
        public IntRange[] AmountOffsets;

        // Chance (0..1) that the change goes up, indexed by Company.
        public float[] RiseChances;

        // Small, medium and large magnitude ranges. Omitted by older development fixtures.
        public IntRange[] AmountBands;
        public MagnitudeWeights[] CompanyWeights;
        public bool UsesAmountBands => AmountBands != null || CompanyWeights != null;

        public void Validate(int companyCount)
        {
            if (RiseChances == null || RiseChances.Length != companyCount)
                throw new FormatException("One rise chance per company is required.");
            foreach (float chance in RiseChances)
                if (float.IsNaN(chance) || float.IsInfinity(chance) || chance < 0 || chance > 1)
                    throw new FormatException("Rise chances must be finite and between 0 and 1.");
            if (UsesAmountBands)
            {
                if (AmountBands == null || AmountBands.Length != 3 || CompanyWeights == null || CompanyWeights.Length != companyCount)
                    throw new FormatException("Three magnitude bands and one weight row per company are required.");
                for (int i = 0; i < AmountBands.Length; i++)
                    if (AmountBands[i] == null || !AmountBands[i].IsValid || AmountBands[i].Min <= 0 ||
                        (i > 0 && AmountBands[i - 1].Max >= AmountBands[i].Min))
                        throw new FormatException("Magnitude bands must be positive, ordered and non-overlapping.");
                foreach (var weights in CompanyWeights)
                    if (weights == null || !ValidChance(weights.Small) || !ValidChance(weights.Medium) || !ValidChance(weights.Large) ||
                        Math.Abs(weights.Small + weights.Medium + weights.Large - 1) > .00001f)
                        throw new FormatException("Magnitude chances must be finite, non-negative and sum to 1.");
            }
            else
            {
                if (Amount == null || !Amount.IsValid || Amount.Min <= 0 || AmountOffsets == null || AmountOffsets.Length != companyCount)
                    throw new FormatException("Legacy magnitude range and company offsets are required.");
                foreach (var offset in AmountOffsets)
                    if (offset == null || !offset.IsValid) throw new FormatException("Invalid magnitude offset.");
            }
        }

        private static bool ValidChance(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
    }

    [Serializable]
    public sealed class MagnitudeWeights
    {
        public float Small;
        public float Medium;
        public float Large;
    }
}
