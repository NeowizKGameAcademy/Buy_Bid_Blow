using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using UnityEngine;

namespace BuyBidBlow.LobbySandbox
{
    public static class DirectConnectionSettings
    {
        public const ushort DefaultPort=7777;
        const string AddressKey="BuyBidBlow.Lobby.Address",PortKey="BuyBidBlow.Lobby.Port";
        public static string LastAddress=>PlayerPrefs.GetString(AddressKey,"localhost");
        public static string LastPort=>PlayerPrefs.GetInt(PortKey,DefaultPort).ToString();
        public static string LocalIPv4()
        {
            try {
                var addresses=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up)
                    .SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address)
                    .Where(a=>a.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)).Select(a=>a.ToString()).Distinct().Take(3).ToArray();
                return addresses.Length>0?string.Join(" / ",addresses):"127.0.0.1";
            }catch(NetworkInformationException){return "PC의 IPv4 주소 확인 필요";}
        }
        public static bool TryPort(string value,out ushort port)=>ushort.TryParse(value?.Trim(),out port)&&port>0;
        public static bool TryAddress(string value,out string address)
        {
            address=(value??"").Trim();
            if(address.ToLowerInvariant()=="localhost"){address="localhost";return true;}
            return IPAddress.TryParse(address,out var ip)&&ip.AddressFamily==AddressFamily.InterNetwork&&ip.ToString()==address&&address!="0.0.0.0"&&address!="255.255.255.255";
        }
        public static void Save(string nickname,string address,ushort port)
        {
            PlayerPrefs.SetString(LobbyNickname.PreferenceKey,nickname);PlayerPrefs.SetString(AddressKey,address);PlayerPrefs.SetInt(PortKey,port);PlayerPrefs.Save();
        }
    }
}
