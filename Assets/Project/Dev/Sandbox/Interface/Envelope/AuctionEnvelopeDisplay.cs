using TMPro;
using UnityEngine;
namespace BuyBidBlow.AuctionDisplay {
 public sealed class AuctionEnvelopeDisplay:MonoBehaviour {
  public Transform rotatingModel;
  public TMP_Text roundLabel;
  [Min(0)] public float degreesPerSecond=20;
  public int firstRound=6,secondRound=9;
  void Start(){RefreshLabel();}
  void Update(){if(rotatingModel)rotatingModel.Rotate(Vector3.up,degreesPerSecond*Time.unscaledDeltaTime,Space.Self);}
  public void SetRounds(int first,int second){firstRound=first;secondRound=second;RefreshLabel();}
  public void RefreshLabel(){if(roundLabel)roundLabel.text=$"{firstRound}R · {secondRound}R";}
 }
}

