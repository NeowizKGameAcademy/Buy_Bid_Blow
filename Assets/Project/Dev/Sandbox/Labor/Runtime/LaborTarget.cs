using BuyBidBlow.Common;
using BuyBidBlow.UI.Notebook;
using Mirror;
using UnityEngine;
namespace BuyBidBlow.Sandbox.Labor
{
    public struct LaborRequest:NetworkMessage { public string Session; public int Target; }
    public struct LaborState:NetworkMessage
    {
        public string Session,Message; public uint[] Owners; public bool[] Available;
        public int[] Adjustments; public int Paid,Bombs; public Vector3 Teleport; public bool Move;
    }
    public sealed class LaborTarget:MonoBehaviour,IInteractable
    {
        public int id;
        public string label;
        public bool CanInteract=>NetworkClient.isConnected&&MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing;
        public string Prompt=>label;
        public bool Interact(GameObject player)
        {
            if(!CanInteract||NetworkNotebookController.BlocksInput)return false;
            NetworkClient.Send(new LaborRequest{Session=LaborRoom.State.Session,Target=id});return true;
        }
    }
}
