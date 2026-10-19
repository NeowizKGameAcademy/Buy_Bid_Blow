using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Menu {
// Editable UI geometry used for numbered annotation badges.
public sealed class DossierCircle : MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;var center=r.center;vh.AddVert(center,color,Vector2.zero);for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;vh.AddVert(center+new Vector2(Mathf.Cos(a)*r.width/2,Mathf.Sin(a)*r.height/2),color,Vector2.zero);if(i>0)vh.AddTriangle(0,i,i+1);}}
}
}
