using TMPro;
using UnityEngine;
namespace BuyBidBlow.Props {
 public sealed class AuctionEnvelopeDisplay:MonoBehaviour {
  public Transform rotatingModel;
  public TMP_Text roundLabel;
  [Min(0)] public float degreesPerSecond=20;
  public int firstRound=6,secondRound=9;
  // 시작 시 라운드 라벨 갱신
  void Start(){RefreshLabel();}
  // 봉투 모델을 일정 속도로 회전
  void Update(){if(rotatingModel)rotatingModel.Rotate(Vector3.up,degreesPerSecond*Time.unscaledDeltaTime,Space.Self);}
  // 라운드 값이 바뀌면 저장하고 라벨 갱신
  public void SetRounds(int first,int second){if(firstRound==first&&secondRound==second)return;firstRound=first;secondRound=second;RefreshLabel();}
  // 라운드 라벨 텍스트 갱신
  public void RefreshLabel(){if(roundLabel){roundLabel.text=$"{firstRound}R · {secondRound}R";roundLabel.ForceMeshUpdate(true);}}
 }
}

