using System.Collections.Generic;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using Mirror;

namespace BuyBidBlow.Characters
{
    // Private persistent state on the player identity. Observers never receive this behaviour's data.
    public sealed class PlayerAssetsNet : NetworkBehaviour
    {
        [SyncVar] private bool received;
        [SyncVar] private int cash, reserved, bet = -1, betStake, secretBid;
        public readonly SyncList<int> Shares = new SyncList<int>();
        public readonly SyncList<Info> Information = new SyncList<Info>();
        [SyncVar(hook = nameof(OnRevisionChanged))] private uint revision;
        public uint Revision => revision;

        private void Awake() => syncMode = SyncMode.Owner;
        public override void OnStartLocalPlayer() { base.OnStartLocalPlayer(); ApplyLocal(); }
        private void OnRevisionChanged(uint _, uint value) => ApplyLocal();

        private void ApplyLocal()
        {
            if (!isLocalPlayer) return;
            var shares = new Dictionary<Company, int>();
            for (int i = 0; i < Shares.Count; i++) shares[(Company)i] = Shares[i];
            PlayerNet.ApplyLocalState(received ? new PortfolioSnapshot(cash, reserved, shares) : null,
                bet >= 0 ? (Company?)bet : null, betStake, secretBid, Information);
        }

        [Server]
        public void Publish(PortfolioSnapshot snapshot, int currentBet, int stake, int submitted, IReadOnlyList<Info> information)
        {
            bool changed = !received || cash != snapshot.Cash || reserved != snapshot.Reserved ||
                bet != currentBet || betStake != stake || secretBid != submitted;
            received = true; cash = snapshot.Cash; reserved = snapshot.Reserved;
            bet = currentBet; betStake = stake; secretBid = submitted;
            for (int i = 0; i < 3; i++)
            {
                int value = snapshot.GetShares((Company)i);
                if (Shares.Count <= i) { Shares.Add(value); changed = true; }
                else if (Shares[i] != value) { Shares[i] = value; changed = true; }
            }
            if (Information.Count != information.Count)
            {
                Information.Clear();
                foreach (var info in information) Information.Add(info);
                changed = true;
            }
            // Written last so the owner's hook sees a complete snapshot, including SyncLists.
            if (changed) revision++;
        }

        [Server]
        public void ClearState()
        {
            received = false; cash = reserved = betStake = secretBid = 0; bet = -1;
            Shares.Clear(); Information.Clear(); revision++;
        }
    }
}
