using UnityEngine;
public class BBBWolfCharacter : MonoBehaviour {
 public enum Face { Happy,Suspicious,Shocked,BlankBluff }
 public Transform model;
 public SkinnedMeshRenderer[] uppers,lowers;
 public SkinnedMeshRenderer tie;
 public GameObject[] faces;
 public GameObject paddle;
 public AnimationClip[] clips;
 public string[] actionLabels;
 [Range(0,2)] public int upper;
 [Range(0,1)] public int lower;
 public Face expression=Face.BlankBluff;
 public Color tieColor=new Color(.54f,.16f,.22f);
 public bool animate;
 public int action;
 [Range(0,1)] public float phase;
 public float speed=1;
 MaterialPropertyBlock block;
 public void SetExpression(Face value){expression=value;ApplyAppearance();}
 public void SetOutfit(int top,int bottom){upper=Mathf.Clamp(top,0,2);lower=Mathf.Clamp(bottom,0,1);ApplyAppearance();}
 public void SetTieColor(Color value){tieColor=value;ApplyAppearance();}
 public void PlayAction(int index){if(clips==null||clips.Length==0)return;action=Mathf.Clamp(index,0,clips.Length-1);phase=0;animate=true;}
 public void ApplyAppearance(){if(uppers!=null)for(int i=0;i<uppers.Length;i++)uppers[i].enabled=i==upper;if(lowers!=null)for(int i=0;i<lowers.Length;i++)lowers[i].enabled=i==lower;if(faces!=null)for(int i=0;i<faces.Length;i++)if(faces[i])faces[i].SetActive(i==(int)expression);if(tie){if(block==null)block=new MaterialPropertyBlock();tie.GetPropertyBlock(block);block.SetColor("_BaseColor",tieColor);tie.SetPropertyBlock(block);}}
 public void Sample(){if(model&&clips!=null&&clips.Length>0){action=Mathf.Clamp(action,0,clips.Length-1);clips[action].SampleAnimation(model.gameObject,phase*clips[action].length);}if(paddle)paddle.SetActive(action==4&&animate);ApplyAppearance();}
 void Awake(){ApplyAppearance();if(paddle)paddle.SetActive(false);}
 void LateUpdate(){if(animate&&clips!=null&&clips.Length>0){phase=Mathf.Repeat(phase+Time.deltaTime*speed/clips[action].length,1);Sample();}else ApplyAppearance();}
 void OnValidate(){ApplyAppearance();}
}
