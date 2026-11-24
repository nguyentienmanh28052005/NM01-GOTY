using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Player;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Interaction
{
    [RequireComponent(typeof(PlayerController), typeof(Interactor))]
    public sealed class CarryController : MonoBehaviour
    {
        [Serializable] class Meta { public float[] col; }
        public sealed class Dropped
        {
            public string kind;
            public GameObject mesh, hit;
            public bool picking;
        }
        public ProductCatalog catalog;
        public bool defaultInteractions = true;
        public string Kind { get; private set; }
        public readonly List<Dropped> DroppedItems = new List<Dropped>();
        public bool Placing => ghost;
        public bool Busy { get; private set; }
        public event Action<string> Changed, PickedUp, Placed;
        PlayerController player;
        Interactor interactor;
        Transform hand;
        GameObject held, ghost, ring, arms;
        Vector3 target;
        float placingTime, carryAnim;
        readonly Dictionary<string, StoreMarker> refs = new Dictionary<string, StoreMarker>();
        readonly List<Material> owned = new List<Material>(), previewMaterials = new List<Material>();
        Material cardboard, tape, blue, cotton, trash, skin, uniform, glow;

        void Start()
        {
            player = GetComponent<PlayerController>(); interactor = GetComponent<Interactor>();
            foreach (var marker in FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!refs.ContainsKey(marker.key)) refs.Add(marker.key, marker);
            hand = new GameObject("Carried item").transform; hand.SetParent(transform, false); hand.localPosition = new Vector3(.32f,-.3f,.55f);
            cardboard = MakeMaterial(0xa98758, .85f); tape=MakeMaterial(0xc8b48a,.35f); blue=MakeMaterial(0x2a6fdb,.4f); cotton=MakeMaterial(0xd9d4c7,.95f);
            trash=MakeMaterial(0x121316,.32f); skin=MakeMaterial(0xe2b998,.6f); uniform=MakeMaterial(0x00a98f,.8f); glow=MakeMaterial(0x5cffd6,.5f);
            arms=new GameObject("Uniform sleeves"); arms.transform.SetParent(transform,false); arms.transform.localPosition=new Vector3(.3f,-.36f,.42f);
            Primitive(arms.transform,PrimitiveType.Capsule,new Vector3(.09f,.205f,.09f),uniform,new Vector3(.03f,-.05f,-.12f)).transform.localRotation=Quaternion.Euler(-90+.25f*Mathf.Rad2Deg,0,0);
            Primitive(arms.transform,PrimitiveType.Sphere,new Vector3(.09f,.063f,.117f),skin,new Vector3(0,0,.06f));
            arms.SetActive(false);
            interactor.Pressed += OnPressed;
        }
        Material MakeMaterial(int hex,float roughness)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color=new Color(((hex>>16)&255)/255f,((hex>>8)&255)/255f,(hex&255)/255f);
            m.SetFloat("_Smoothness",1-roughness); owned.Add(m); return m;
        }
        static GameObject Primitive(Transform parent,PrimitiveType type,Vector3 scale,Material material,Vector3 position)
        {
            var go=GameObject.CreatePrimitive(type); go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        GameObject Visual(string kind,bool inHand)
        {
            var root=new GameObject(kind);
            if(kind=="boxRamen" || kind=="boxMilk")
            {
                Primitive(root.transform,PrimitiveType.Cube,inHand?new Vector3(.35f,.22f,.3f):new Vector3(.5f,.35f,.45f),cardboard,Vector3.zero);
                if(!inHand) Primitive(root.transform,PrimitiveType.Cube,new Vector3(.5f,.004f,.06f),tape,new Vector3(0,.176f,0));
            }
            else if(kind=="mop")
            {
                var shaft=Primitive(root.transform,PrimitiveType.Cylinder,new Vector3(.028f,.65f,.028f),blue,Vector3.zero);
                shaft.transform.localRotation=Quaternion.Euler(0,0,90);
                var head=Primitive(root.transform,PrimitiveType.Cylinder,new Vector3(.18f,.08f,.18f),cotton,new Vector3(.72f,0,0));head.transform.localRotation=Quaternion.Euler(0,0,90);
                for(int strand=0;strand<12;strand++)
                {
                    float a=strand*Mathf.PI/6;
                    Primitive(root.transform,PrimitiveType.Capsule,new Vector3(.022f,.08f,.022f),cotton,new Vector3(.76f,Mathf.Cos(a)*.06f,Mathf.Sin(a)*.06f)).transform.localRotation=Quaternion.Euler(0,0,90);
                }
                if(inHand)root.transform.localRotation=Quaternion.Euler(0,0,-65);
            }
            else if(kind=="trash") Primitive(root.transform,PrimitiveType.Sphere,new Vector3(.4f,.28f,.4f),trash,Vector3.zero);
            else
            {
                var entry=catalog?catalog.Find("kimbap"):null;
                if(entry!=null && entry.prefabs.Length>0)
                {
                    var product=Instantiate(entry.prefabs[0],root.transform); var bounds=new Bounds();bool first=true;
                    foreach(var r in product.GetComponentsInChildren<Renderer>()) {if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
                    float s=.19f/Mathf.Max(.001f,Mathf.Max(bounds.size.x,bounds.size.z));product.transform.localScale*=s;
                    product.transform.localPosition-=bounds.center*s;
                }
            }
            return root;
        }
        public static string DisplayName(string kind)
        {
            switch(kind)
            {
                case "boxRamen":return "Thùng mì ly";case "boxMilk":return "Thùng bánh mandu";case "mop":return "Cây lau nhà";
                case "trash":return "Túi rác";case "heat":return "Cơm cuộn (cần hâm)";case "hot":return "Cơm cuộn nóng";default:return "";
            }
        }
        public void SetCarry(string kind)
        {
            CancelPlacing();if(held)Destroy(held);Kind=kind;carryAnim=0;
            if(!string.IsNullOrEmpty(kind)) {held=Visual(kind,true);held.transform.SetParent(hand,false);}
            arms.SetActive(!string.IsNullOrEmpty(kind));Changed?.Invoke(kind);
        }
        void OnPressed(string id) {if(defaultInteractions)Use(id,interactor.Current);}
        public void Use(string id,StoreMarker hit)
        {
            if(Busy)return;
            if(id=="dropped" && string.IsNullOrEmpty(Kind))
            {
                var dropped=DroppedItems.FirstOrDefault(d=>d.hit==hit.gameObject);
                if(dropped!=null)StartCoroutine(Pickup(dropped));
            }
            else if(id=="mop" && Kind=="mop") {SetCarry(null);SetSourcePresent("R:mop",true);}
            else if(string.IsNullOrEmpty(Kind))
            {
                string key=id=="trash"?"S:trashBag":id=="mop"?"R:mop":id=="boxRamen"?"R:boxRamen":id=="boxMilk"?"R:boxMilk":null;
                if(key!=null && refs.TryGetValue(key,out var source) && source.gameObject.activeSelf)
                {SetSourcePresent(key,false);SetCarry(id);PickedUp?.Invoke(id);}
            }
        }
        public void SetSourcePresent(string key,bool present)
        {
            if(!refs.TryGetValue(key,out var source))return;
            source.gameObject.SetActive(present);
            var meta=string.IsNullOrEmpty(source.json)?null:JsonUtility.FromJson<Meta>(source.json);
            if(meta?.col==null)return;
            foreach(var box in player.collisionBoxes)
                if(Mathf.Abs(box.x0-meta.col[0])<.001f && Mathf.Abs(box.z0-meta.col[1])<.001f)box.off=!present;
            foreach(var m in refs.Values.Where(m=>m.kind==StoreMarker.Kind.Collider))
            {
                var c=m.GetComponent<BoxCollider>(); var p=ThreeCoords.ToUnity(m.transform.position);
                if(c && Mathf.Abs(p.x-(meta.col[0]+meta.col[2])*.5f)<.001f && Mathf.Abs(p.z-(meta.col[1]+meta.col[3])*.5f)<.001f)c.enabled=present;
            }
        }
        void Update()
        {
            float dt=Time.deltaTime;carryAnim=Mathf.Min(1,carryAnim+dt*4.5f);
            if(hand)hand.localPosition=new Vector3(.32f,-.3f-(1-carryAnim)*.25f,.55f);
            if(!player || !player.Input || Busy || !player.GameplayEnabled || string.IsNullOrEmpty(Kind)) {CancelPlacing();return;}
            var drop=player.Input.FindAction("Drop");
            if(drop.WasPressedThisFrame()) BeginPlacing();
            if(ghost)
            {
                placingTime+=dt; var camera=GetComponent<Camera>();
                target=Placement.Target(new Ray(ThreeCoords.ToUnity(camera.transform.position),ThreeCoords.ToUnity(camera.transform.forward)),player.Motor.pos,player.Area);
                ghost.transform.position=ThreeCoords.ToUnity(target+Vector3.up*HalfHeight(Kind));ghost.transform.rotation=Quaternion.Euler(0,-player.Motor.yaw*Mathf.Rad2Deg+(Kind=="mop"?-90:0),0);
                ring.transform.position=ThreeCoords.ToUnity(target+Vector3.up*.006f);ring.transform.localScale=new Vector3(.48f,1,.48f)*(1+Mathf.Sin(placingTime*6)*.06f);
                if(!drop.IsPressed())Drop(target);
            }
        }
        public static float HalfHeight(string kind) => kind=="boxRamen"||kind=="boxMilk" ? .175f : kind=="trash" ? .14f : kind=="mop" ? .06f : .01f;
        public void BeginPlacing()
        {
            if(string.IsNullOrEmpty(Kind)||Busy||ghost)return;
            ghost=Visual(Kind,false);placingTime=0;
            foreach(var r in ghost.GetComponentsInChildren<Renderer>())
            {
                var mats=r.sharedMaterials.Select(m=>new Material(m)).ToArray();r.sharedMaterials=mats;
                foreach(var m in mats)
                {previewMaterials.Add(m);m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;var c=m.color;c.a=.38f;m.color=c;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(0,.35f,.26f));}
            }
            ring=new GameObject("Placement ring");var line=ring.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.widthMultiplier=.012f;line.positionCount=40;line.sharedMaterial=glow;
            for(int i=0;i<40;i++){float a=i*Mathf.PI/20;line.SetPosition(i,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)));}
        }
        public void CancelPlacing()
        {
            if(ghost)Destroy(ghost);if(ring)Destroy(ring);ghost=null;ring=null;
            foreach(var m in previewMaterials)if(m)Destroy(m);previewMaterials.Clear();
        }
        public void Drop(Vector3 threeTarget)
        {
            if(string.IsNullOrEmpty(Kind)||Busy)return;
            string kind=Kind;var visual=Visual(kind,false);visual.transform.position=hand.position;
            var destination=threeTarget+Vector3.up*HalfHeight(kind);SetCarry(null);
            StartCoroutine(DropArc(kind,visual,destination));
        }
        IEnumerator DropArc(string kind,GameObject visual,Vector3 destination)
        {
            Vector3 from=ThreeCoords.ToUnity(visual.transform.position);
            for(float t=0;t<.3f;t+=Time.deltaTime){visual.transform.position=ThreeCoords.ToUnity(Placement.Arc(from,destination,t/.3f,.18f));yield return null;}
            visual.transform.position=ThreeCoords.ToUnity(destination);
            var hit=new GameObject("H:dropped");hit.layer=LayerMask.NameToLayer("Interact");hit.transform.position=ThreeCoords.ToUnity(destination+Vector3.up*(.25f-HalfHeight(kind)));
            var collider=hit.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=new Vector3(.6f,.5f,.6f);
            var marker=hit.AddComponent<StoreMarker>();marker.kind=StoreMarker.Kind.Hit;marker.key="H:dropped";marker.json=JsonUtility.ToJson(new DropInfo{label="Nhặt: "+DisplayName(kind)});
            var record=new Dropped{kind=kind,mesh=visual,hit=hit};DroppedItems.Add(record);Placed?.Invoke(kind);
            var scale=visual.transform.localScale;float squash=kind=="boxRamen"||kind=="boxMilk"||kind=="trash" ? .82f : .9f;
            for(float t=0;t<.22f;t+=Time.deltaTime){if(!visual || record.picking)yield break;float k=Mathf.Clamp01(t/.22f),sy=1+(squash-1)*Mathf.Sin(k*Mathf.PI)*(1-k*.4f);visual.transform.localScale=Vector3.Scale(scale,new Vector3(1+(1-sy)*.5f,sy,1+(1-sy)*.5f));yield return null;}
            if(visual && !record.picking)visual.transform.localScale=scale;
        }
        [Serializable] class DropInfo {public string id="dropped",label;}
        IEnumerator Pickup(Dropped dropped)
        {
            Busy=true;dropped.picking=true;DroppedItems.Remove(dropped);Destroy(dropped.hit);
            var from=ThreeCoords.ToUnity(dropped.mesh.transform.position);var scale=dropped.mesh.transform.localScale;
            for(float t=0;t<.18f;t+=Time.deltaTime){dropped.mesh.transform.position=ThreeCoords.ToUnity(Placement.Arc(from,ThreeCoords.ToUnity(hand.position),t/.18f,.08f));dropped.mesh.transform.localScale=scale*Mathf.Lerp(1,.6f,t/.18f);yield return null;}
            Destroy(dropped.mesh);SetCarry(dropped.kind);Busy=false;PickedUp?.Invoke(dropped.kind);
        }
        void OnDestroy()
        {
            if(interactor)interactor.Pressed-=OnPressed;CancelPlacing();
            foreach(var item in DroppedItems){if(item.mesh)Destroy(item.mesh);if(item.hit)Destroy(item.hit);}
            foreach(var m in owned)if(m)Destroy(m);
        }
    }
}
