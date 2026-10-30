using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Auction
{
    // Builds every lot of the match up front, then hands them out in order.
    public sealed class LotGenerator
    {
        public const int RoundsPerAuction = 4;

        private readonly Queue<Lot> lots = new Queue<Lot>();

        public int Remaining => lots.Count;

        // 경매마다 4개 라운드를 섞어 물건 2개씩 미리 생성해 큐에 저장
        // prices[n][company] is the close of round n; prices[0] holds the starting prices.
        // Pass the match's Random so one seed replays prices and lots together.
        public LotGenerator(IReadOnlyList<int> auctionRounds, IReadOnlyList<int[]> prices, Random random, bool currentRoundOnly = false)
        {
            int companyCount = Enum.GetValues(typeof(Company)).Length;

            foreach (int start in auctionRounds)
            {
                if (currentRoundOnly)
                {
                    if (start < 1 || start >= prices.Count)
                        throw new ArgumentOutOfRangeException(nameof(auctionRounds));
                    lots.Enqueue(new Lot(Make(start, prices, companyCount, random)));
                    continue;
                }
                if (start < 1 || start + RoundsPerAuction - 1 >= prices.Count)
                    throw new ArgumentOutOfRangeException(nameof(auctionRounds), $"Auction at round {start} needs prices up to round {start + RoundsPerAuction - 1}.");

                var rounds = new int[RoundsPerAuction];
                for (int i = 0; i < rounds.Length; i++)
                    rounds[i] = start + i;
                Shuffle(rounds, random);

                lots.Enqueue(Create(rounds[0], rounds[1], prices, companyCount, random));
                lots.Enqueue(Create(rounds[2], rounds[3], prices, companyCount, random));
            }
        }

        // 다음 물건을 꺼내 반환
        public Lot Next()
        {
            if (lots.Count == 0)
                throw new InvalidOperationException("No lots left.");

            return lots.Dequeue();
        }

        // 두 라운드 정보를 라운드 순으로 묶어 물건을 생성
        private static Lot Create(int a, int b, IReadOnlyList<int[]> prices, int companyCount, Random random)
        {
            Info first = Make(Math.Min(a, b), prices, companyCount, random);
            Info second = Make(Math.Max(a, b), prices, companyCount, random);
            return new Lot(first, second);
        }

        // 서로 다른 두 회사를 골라 해당 라운드의 등락 정보를 생성
        // Two different companies per round; the other rounds may repeat the same pair.
        private static Info Make(int round, IReadOnlyList<int[]> prices, int companyCount, Random random)
        {
            int a = random.Next(companyCount);
            int b = random.Next(companyCount - 1);
            if (b >= a) b++;  // skip a so the two always differ

            var first = (Company)Math.Min(a, b);
            var second = (Company)Math.Max(a, b);
            return new Info(round, first, GetDirection(round, first, prices), second, GetDirection(round, second, prices));
        }

        // 이전 라운드 대비 종가 차이로 상승·하락·보합을 판정
        // Real closes, floor included, so a fall stopped at the floor reads as Flat.
        private static Direction GetDirection(int round, Company company, IReadOnlyList<int[]> prices)
        {
            int diff = prices[round][(int)company] - prices[round - 1][(int)company];
            return diff > 0 ? Direction.Rise : diff < 0 ? Direction.Fall : Direction.Flat;
        }

        // 배열을 무작위로 섞음
        private static void Shuffle(int[] values, Random random)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
