using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Hall
{
    [RequireComponent(typeof(CharacterController),typeof(HallSeatActor))]
    public sealed class HallWalkthrough : MonoBehaviour
    {
        public Camera view;
        public float speed=4f;
        CharacterController motor; HallSeatActor actor; float pitch,fall;
        void Start()
        {
            motor=GetComponent<CharacterController>();actor=GetComponent<HallSeatActor>();
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            var contact=gameObject.AddComponent<CapsuleCollider>();contact.center=Vector3.up*.85f;contact.height=1.7f;contact.radius=.3f;contact.isTrigger=true;
            foreach(var rope in FindObjectsByType<HallPhysicsRope>(FindObjectsSortMode.None))rope.contacts=new[]{contact};
            foreach(var banner in FindObjectsByType<HallBannerWind>(FindObjectsSortMode.None))banner.SetContacts(new[]{contact});
        }
        void Update()
        {
            var k=Keyboard.current;var m=Mouse.current;if(k==null)return;
            if(k.escapeKey.wasPressedThisFrame){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(m!=null && m.leftButton.wasPressedThisFrame){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            if(m!=null){var delta=m.delta.ReadValue()*.1f;if(!actor.IsSeated)transform.Rotate(0,delta.x,0);pitch=Mathf.Clamp(pitch-delta.y,-75,75);view.transform.localRotation=Quaternion.Euler(pitch,0,0);}
            if(k.eKey.wasPressedThisFrame)HallSeatActor.TryInteract(transform,view.transform);
            if(actor.IsSeated){fall=0;return;}
            var input=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));input=Vector2.ClampMagnitude(input,1);
            if(motor.isGrounded)fall=-2;else fall+=Physics.gravity.y*Time.deltaTime;
            motor.Move(((transform.right*input.x+transform.forward*input.y)*speed+Vector3.up*fall)*Time.deltaTime);
        }
        void OnGUI()
        {
            string text="WASD: move   Mouse: look   E: interact   Esc: release cursor";
            if(actor && actor.IsSeated)text="E: stand up";
            else if(view && Physics.Raycast(view.transform.position,view.transform.forward,out var hit,3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {var target=hit.collider.GetComponentInParent<HallInteractable>();if(target)text="E: "+target.Prompt;}
            GUI.Label(new Rect(20,20,650,30),text);GUI.Label(new Rect(Screen.width/2-4,Screen.height/2-10,20,20),"+");
        }
    }
}
