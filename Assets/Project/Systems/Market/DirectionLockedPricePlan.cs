using System;
using System.Collections.Generic;

namespace BuyBidBlow.Systems.Market
{
    // Original signed changes are immutable: auction lots keep their promised directions.
    public sealed class DirectionLockedPricePlan
    {
        readonly int[][] changes;
        readonly int[][] futureFalls;
        readonly int minimum;
        public DirectionLockedPricePlan(IReadOnlyList<int[]> plan,int minimum)
        {
            this.minimum=minimum;
            changes=new int[plan.Count][];futureFalls=new int[plan.Count][];
            int companies=plan[0].Length;
            for(int r=0;r<plan.Count;r++)
            {
                changes[r]=new int[companies];futureFalls[r]=new int[companies];
                if(r>0)for(int c=0;c<companies;c++)changes[r][c]=plan[r][c]-plan[r-1][c];
            }
            for(int r=plan.Count-2;r>=0;r--)
                for(int c=0;c<companies;c++)
                    futureFalls[r][c]=changes[r+1][c]>0?0:futureFalls[r+1][c]+(changes[r+1][c]<0?1:0);
        }
        public void Apply(IList<int[]> prices,int round,int company,int adjustment)
        {
            // Reproject only unrevealed closes. Later rounds retain their original direction,
            // and get no labour adjustment of their own until work is done in that round.
            for(int r=round;r<prices.Count;r++)
                prices[r][company]=Resolve(prices[r-1][company],changes[r][company],r==round?adjustment:0,
                    checked(minimum+futureFalls[r][company]));
        }
        public static int Resolve(int previous,int plannedChange,int adjustment,int safeMinimum)
        {
            if(plannedChange==0)return previous;
            long desired=(long)previous+plannedChange+adjustment;
            if(plannedChange>0)
                return checked((int)Math.Max(Math.Max((long)previous+1,safeMinimum),desired));
            // Reserve one unit per promised fall before the next rise. Simply clamping to the
            // floor could turn a later sold 'fall' into 'flat'. The previous step preserves this buffer.
            if(previous<=safeMinimum)throw new InvalidOperationException("No room for the promised falling direction.");
            return checked((int)Math.Max(safeMinimum,Math.Min((long)previous-1,desired)));
        }
    }
}
