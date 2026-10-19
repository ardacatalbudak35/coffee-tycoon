using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace CafeLife
{
    public sealed class TouchPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public bool look; public Vector2 value; public RectTransform knob;
        private int pointer=int.MinValue;
        public void OnPointerDown(PointerEventData e) { if(pointer!=int.MinValue) return; pointer=e.pointerId; OnDrag(e); }
        public void OnDrag(PointerEventData e)
        {
            if(pointer!=e.pointerId) return;
            if(look) { value+=e.delta; return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var p);
            value=Vector2.ClampMagnitude(p/65f,1); if(knob) knob.anchoredPosition=value*65;
        }
        public void OnPointerUp(PointerEventData e) { if(pointer==e.pointerId) ResetPad(); }
        public void ResetPad() { pointer=int.MinValue; value=Vector2.zero; if(knob) knob.anchoredPosition=Vector2.zero; }
        private void OnDisable() => ResetPad();
    }
    public sealed class TouchSprint : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool held; private int pointer=int.MinValue;
        public void OnPointerDown(PointerEventData e) { if(pointer!=int.MinValue)return;pointer=e.pointerId;held=true; }
        public void OnPointerUp(PointerEventData e) { if(pointer==e.pointerId) { pointer=int.MinValue; held=false; } }
        public void OnPointerExit(PointerEventData e) { if(pointer==e.pointerId) { pointer=int.MinValue; held=false; } }
        public void ResetPad() {pointer=int.MinValue;held=false;}
        private void OnDisable() => ResetPad();
    }
    public sealed class CafePlayer : MonoBehaviour
    {
        public Camera view; public CharacterController body; public TouchPad movePad,lookPad; public TouchSprint sprintPad;
        public CafeGame game; public Transform hand; public float pitch;
        private float fall; private Vector3 spawn; private bool jumpRequested;
        public string Hint { get; private set; }
        public bool PointerMode { get; private set; } = true;
        public CafeTarget target;
        void Start() { spawn=transform.position; }
        public void Lock(bool locked)
        {
            PointerMode=!locked;jumpRequested=false;
            Cursor.lockState=locked && !Application.isMobilePlatform ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible=!locked || Application.isMobilePlatform;
            if(movePad) movePad.ResetPad(); if(lookPad) lookPad.ResetPad(); if(sprintPad)sprintPad.ResetPad();
        }
        public void TogglePointer(){if(game==null||game.Paused||Application.isMobilePlatform)return;var pour=game.GetComponent<CafeLattePour>();if(pour&&pour.Active)return;Lock(PointerMode);}
        public bool OverInterface(Vector2 position){if(!EventSystem.current)return false;var data=new PointerEventData(EventSystem.current){position=position};var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);return hits.Count>0;}
        void Update()
        {
            if(game==null) return;
            var pour=game.GetComponent<CafeLattePour>();if(pour&&pour.Active){Hint="Süt dökümü";return;}
            var k=Keyboard.current;
            if(CafeFocusedMode.CounterControls){if(k!=null&&k.escapeKey.wasPressedThisFrame)game.TogglePause();Hint="";return;}
            if(k!=null && k.escapeKey.wasPressedThisFrame) game.TogglePause();
            if(game.Paused) { Hint=""; return; }
            if(k!=null && k.qKey.wasPressedThisFrame){TogglePointer();return;}
            if(k!=null && k.tabKey.wasPressedThisFrame){game.OpenOperations();return;}
            if(k!=null && k.rKey.wasPressedThisFrame){game.OpenOperations("recipes");return;}
            if(k!=null && k.f1Key.wasPressedThisFrame){game.OpenOperations("help");return;}
            if(k!=null && k.mKey.wasPressedThisFrame) { game.OpenDistrict(); return; }
            if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState!=CursorLockMode.Locked && !Application.isMobilePlatform && !OverInterface(Mouse.current.position.ReadValue())) Lock(true);
            Vector2 look=Vector2.zero;
            if(Mouse.current!=null && Cursor.lockState==CursorLockMode.Locked) look=Mouse.current.delta.ReadValue()*game.data.sensitivity;
            if(lookPad) { look+=lookPad.value*.13f; lookPad.value=Vector2.zero; }
            transform.Rotate(0,look.x,0); pitch=Mathf.Clamp(pitch-look.y,-80,80); view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            Vector2 input=movePad ? movePad.value : Vector2.zero;
            if(k!=null) input+=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0), (k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
            input=Vector2.ClampMagnitude(input,1);
            bool sprint=(k!=null&&(k.leftShiftKey.isPressed||k.rightShiftKey.isPressed))||(sprintPad&&sprintPad.held);
            bool jump=(k!=null&&k.spaceKey.wasPressedThisFrame)||jumpRequested; jumpRequested=false;
            Move(input,Time.deltaTime,sprint,jump);
            RefreshTarget();
            if(k!=null && k.eKey.wasPressedThisFrame) Use();
        }
        public void RefreshTarget()
        {
            target=null; Hint="";var basketball=game.GetComponent<CafeBasketball>();if(basketball&&basketball.Held){Hint="Potaya nişan al · E / ETKİLEŞ: at · Basket +50 coin";return;}
            // Aim at generous interaction volumes instead of the tiny mesh colliders.
            // This avoids the counter or decorative meshes stealing the E prompt.
            var hits=Physics.SphereCastAll(view.transform.position,.075f,view.transform.forward,3.4f,1<<8,QueryTriggerInteraction.Collide);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                var candidate=hit.collider.GetComponentInParent<CafeTarget>();
                if(candidate&&candidate.kind=="casino"&&(game.GetComponent<CafeTableBlackjack>()?.Opened??false))continue;
                if(candidate&&candidate.kind!=null&&candidate.kind.StartsWith("blackjack_")&&Mathf.Abs(transform.position.y-4.2f)>1.2f)continue;
                if(candidate&&candidate.kind=="casino"&&Mathf.Abs(transform.position.y-candidate.transform.position.y)>1.1f)continue;
                if(candidate) { target=candidate; break; }
            }
            if(!target)target=CafeActivityArea.Nearby(game);
            if(target) Hint=target.Label(game);
        }
        public bool WorldInputBlocked=>!game||game.Paused||(game.GetComponent<CafeLattePour>()?.Active??false);
        public void Jump() { if(!WorldInputBlocked)jumpRequested=true; }
        public void Move(Vector2 input,float dt,bool sprint=false,bool jump=false)
        {
            if(game.GetComponent<CafeTableBlackjack>()?.Opened??false)return;
            if(game.expansion && game.expansion.Seated){if(jump)game.expansion.ToggleSit();return;}
            if(game.expansion){game.expansion.Stamina(sprint,input.sqrMagnitude>.01f,dt);sprint=sprint&&game.expansion.Data.stamina>0;}
            if(body.isGrounded) { if(fall<0) fall=-2; if(jump) fall=6.2f; }
            fall=Mathf.Max(-30,fall-20*dt);
            float speed=(game.data.hundred!=null?game.data.hundred.walkSpeed:3.4f)*(sprint?1.4f:1);
            body.Move((transform.forward*input.y*speed+transform.right*input.x*speed+Vector3.up*fall)*dt);
            if(transform.position.y < -5) { body.enabled=false; transform.position=spawn; body.enabled=true; fall=0; }
        }
        public void Use() { if(WorldInputBlocked)return;var basketball=game.GetComponent<CafeBasketball>();if(basketball&&basketball.Held){basketball.Throw();return;}
            if(game.expansion && game.expansion.Seated){game.expansion.ToggleSit();return;} if(!game.Paused && target) target.Use(game); }
        void OnApplicationFocus(bool focus) { if(!focus && game && !game.testing && !game.Paused) game.TogglePause(); }
        void OnDisable() { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
    }
    public sealed class CafeTarget : MonoBehaviour
    {
        public string kind; public CafeCustomer customer;
        public int branchIndex; public CafeVenue venue; public CafeLitter litter;
        public string Label(CafeGame g)
        {
            if(kind!=null&&kind.StartsWith("blackjack_"))return "Blackjack · "+(kind=="blackjack_hit"?"Kart çek":kind=="blackjack_stand"?"Dur":kind=="blackjack_leave"?"Masadan ayrıl":kind.Substring(10)+" coin oyna")+" [E]";
            if(litter)return litter.table?"Masayı temizle [E]":"Sokak çöpünü topla · +2 coin [E]";
            if(kind=="foam")return "Süt köpürt [E]"; if(kind=="art")return "Latte art · zamanlamayı yakala [E]"; if(kind=="clean_machine")return "Makineyi temizle · 5 coin [E]"; if(kind=="clean_cafe")return "Kafeyi temizle [E]"; if(kind=="taxi")return "Taksi · mahalle yolculuğu [E]"; if(kind=="bench")return "Bankta otur / kalk [E]";
            if(venue) return venue.title+" · Hizmetleri gör [E]";
            if(customer) return customer.Label(g);
            if(kind=="cup_hot") return "Sıcak karton bardak al [E]";
            if(kind=="cup_cold") return "Soğuk plastik bardak al [E]";
            if(kind=="lid_hot") return "Karton kapağı tak [E]";
            if(kind=="lid_cold") return "Plastik kapağı tak [E]";
            if(kind=="cup") return "Bardak al [E]";
            if(kind=="branch") return "Kafenin durumunu / kirayı gör [E]";
            if(kind=="branch_exit") return "Şube kapısından şehre çık [E]";
            if(kind=="boulevard_ranking")return "Cadde ligi · en iyi kafeler [E]";if(kind=="boulevard_arcade")return "Topu al · Basket +50 coin [E]";if(kind=="boulevard_market")return "Gece marketi · tedarik [E]";
            if(kind=="casino") return "Blackjack masası • oyna [E]";
            if(kind=="ice") return "Buz makinesinden bardağa buz koy [E]";
            if(kind=="grinder") return "Espresso çekirdeğini öğüt [E]";
            if(kind=="machine") return g.brewTime>0 ? "Espresso · yeşil aralıkta durdur [E]" : g.CupOnMachine ? "Bardağı makineden al [E]" : "Espressoyu demle [E]";
            if(kind=="milk") return "Süt dolabından süt ekle [E]";
            if(kind=="water") return "Arıtma çeşmesinden su ekle [E]";
            if(kind=="caramel") return "Karamel şurubu ekle [E]";
            if(kind=="vanilla") return "Vanilya şurubu ekle [E]";
            if(kind=="lid") return "Uygun kapağı tak [E]";
            if(kind=="pickup") return "Kapaklı ürünü bar sonuna bırak [E] • Sipariş gerekmez";
            if(kind=="depot") return "Depodan bütün eksik ürünleri bara taşı [E]";
            if(kind=="trash") return "Eldeki ürünü at [E]";
            return "Kafe yönetimi / stok / dekor [E]";
        }
        public void Use(CafeGame g)
        { if(kind!=null&&kind.StartsWith("blackjack_"))g.GetComponent<CafeTableBlackjack>().Action(kind.Substring(10));else if(kind=="boulevard_ranking")g.OpenStreetPage("ranking");else if(kind=="boulevard_arcade")g.GetComponent<CafeBasketball>().Interact();else if(kind=="boulevard_market")g.OpenOperations("supply");else if(litter)litter.Clean();else if(venue) g.OpenVenue(venue); else if(customer) customer.Interact(g); else if(kind=="branch")g.OpenBranch(branchIndex); else if(kind=="branch_exit")g.ExitBranch(); else if(kind=="casino")g.OpenCasino(); else g.Act(kind); }
    }
}





