using UnityEngine;

namespace BuyBidBlow.UI.Lobby
{
    public sealed class LobbySpriteLibrary : ScriptableObject
    {
        [System.Serializable] public class Entry
        {
            public string id;
            public Sprite sprite;
        }
        public Entry[] entries;
        public Sprite Find(string id)
        {
            if(entries!=null) foreach(var entry in entries) if(entry.id==id)return entry.sprite;
            return null;
        }
    }
}
