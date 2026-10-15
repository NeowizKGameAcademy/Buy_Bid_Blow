using System.Linq;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.LobbySandbox
{
    public sealed class LobbyRoom : NetworkManager
    {
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            var used = NetworkServer.connections.Values.Where(c => c.identity).Select(c => c.identity.GetComponent<LobbyPlayer>().seat).ToArray();
            int seat = Enumerable.Range(0,6).FirstOrDefault(i => !used.Contains(i));
            if (used.Length >= 6) { conn.Disconnect(); return; }
            var player = Instantiate(playerPrefab);
            player.GetComponent<LobbyPlayer>().seat = seat;
            NetworkServer.AddPlayerForConnection(conn,player);
        }
    }
}
