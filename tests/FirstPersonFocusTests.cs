// The production focus module, picker, math and patch entry points run against
// these small game/Unity doubles. Quaternion math is real; physics hits and the
// native pose producer are controlled fixtures, not an in-game rendering test.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.Cameras;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public static explicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public Vector3 normalized { get { return this / (float)Math.Sqrt(sqrMagnitude); } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float b) { return new Vector3(a.x * b, a.y * b, a.z * b); }
        public static Vector3 operator /(Vector3 a, float b) { return new Vector3(a.x / b, a.y / b, a.z / b); }
    }
    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity { get { return new Quaternion(0, 0, 0, 1); } }
        public Quaternion normalized
        {
            get { float n = (float)Math.Sqrt(x*x + y*y + z*z + w*w); return n > 0 ? new Quaternion(x/n,y/n,z/n,w/n) : identity; }
        }
        private static Quaternion Axis(float degrees, int axis)
        {
            double half = degrees * Math.PI / 360;
            float s = (float)Math.Sin(half), c = (float)Math.Cos(half);
            return new Quaternion(axis == 0 ? s : 0, axis == 1 ? s : 0, axis == 2 ? s : 0, c);
        }
        public static Quaternion Euler(float x, float y, float z) { return Axis(y,1) * Axis(x,0) * Axis(z,2); }
        public static Quaternion Inverse(Quaternion q)
        {
            float n = q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w;
            return new Quaternion(-q.x/n,-q.y/n,-q.z/n,q.w/n);
        }
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y, a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,
                a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w, a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z);
        }
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            Quaternion r = q * new Quaternion(v.x,v.y,v.z,0) * Inverse(q);
            return new Vector3(r.x,r.y,r.z);
        }
    }
    public static class Mathf
    {
        public const float Rad2Deg = (float)(180 / Math.PI);
        public static float Sqrt(float x) { return (float)Math.Sqrt(x); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Max(float x, float y) { return Math.Max(x, y); }
        public static float Min(float x, float y) { return Math.Min(x, y); }
        public static float Clamp(float x, float lo, float hi) { return Math.Max(lo, Math.Min(x, hi)); }
        public static float DeltaAngle(float a, float b)
        {
            float d = b-a; d -= (float)Math.Floor(d / 360) * 360; return d > 180 ? d-360 : d;
        }
    }
    public class GameObject { public int layer; public Transform transform = new Transform(); }
    public class Transform
    {
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Transform parent;
        public bool IsChildOf(Transform other)
        {
            for (Transform t = this; t != null; t = t.parent) if (t == other) return true;
            return false;
        }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x=x; this.y=y; width=w; height=h; }
        public Vector2 center { get { return new Vector2(x+width/2,y+height/2); } }
        public bool Contains(Vector2 p) { return p.x>=x && p.y>=y && p.x<x+width && p.y<y+height; }
    }
    public struct Ray { public Vector3 origin, direction; }
    public class Camera
    {
        public Rect pixelRect = new Rect(0,0,1920,1080);
        public float fieldOfView = 51;
        public Vector2 LastRayPosition;
        public Ray ScreenPointToRay(Vector2 p) { LastRayPosition=p; return new Ray { direction = Vector3.forward }; }
    }
    public class Collider
    {
        public GameObject gameObject = new GameObject();
        public Transform transform { get { return gameObject.transform; } }
        public bool isTrigger;
        public readonly Dictionary<Type, object> Components = new Dictionary<Type, object>();
        public T GetComponentInParent<T>() where T : class
        {
            object value; return Components.TryGetValue(typeof(T),out value) ? (T)value : null;
        }
    }
    public struct RaycastHit { public Collider collider; public Vector3 point; public float distance; }
    public enum QueryTriggerInteraction { Collide }
    public static class Physics
    {
        public static RaycastHit[] Hits = new RaycastHit[0];
        public static int Calls;
        public static RaycastHit[] RaycastAll(Ray r,float d,int mask,QueryTriggerInteraction q) { Calls++; return Hits; }
    }
    public enum CursorLockMode { None, Locked }
    public static class Cursor { public static CursorLockMode lockState; }
    public static class Application { public static bool isFocused = true; }
    public static class Time { public static int frameCount; public static float unscaledDeltaTime = 1f/60; }
    public enum KeyCode { None, Minus, KeypadMinus, Backspace }
    public static class Input
    {
        public static Vector3 mousePosition = new Vector3(123,456,0);
        public static bool MiddleDown, MiddleHeld, MiddleUp;
        public static KeyCode DownKey;
        public static bool GetMouseButtonDown(int b) { return b==2 && MiddleDown; }
        public static bool GetMouseButton(int b) { return b==2 && MiddleHeld; }
        public static bool GetMouseButtonUp(int b) { return b==2 && MiddleUp; }
        public static bool GetKeyDown(KeyCode k) { return k != KeyCode.None && DownKey==k; }
    }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple=true)]
    public class HarmonyPatch : Attribute
    {
        public Type type; public string method;
        public HarmonyPatch() { }
        public HarmonyPatch(Type type, string method) { this.type=type; this.method=method; }
    }
    public class HarmonyPrefix : Attribute { }
    public class HarmonyPostfix : Attribute { }
    public static class AccessTools
    {
        public static FieldInfo Field(Type t, string n)
        {
            for (;t!=null;t=t.BaseType)
            {
                FieldInfo f=t.GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
                if(f!=null)return f;
            }
            return null;
        }
    }
}
namespace Assets.Scripts
{
    public static class Layers
    {
        public const int DefaultLayer=0, CarLayer=1, AircraftInteractable=2, TerrainLayer=3,
            AircraftLayer=4, CarrierDeck=5, AircraftCollisionOnly=6, AircraftCollisionNone=7, RemoteAircraftLayer=8;
    }
    public static class Utility
    {
        public static Vector3 Origin;
        public static Vector3 ConvertFloatingOriginToAbsolutePosition(Vector3 p) { return p+Origin; }
        public static Vector3 ConvertAbsoluteToFloatingOriginPosition(Vector3 p) { return p-Origin; }
    }
}
namespace Assets.Scripts.Craft
{
    public class TargetingSystem { public Assets.Scripts.Flight.Combat.Target CurrentTarget; }
    public class AircraftScript
    {
        public Transform transform = new Transform(); public TargetingSystem TargetingSystem = new TargetingSystem();
    }
}
namespace Assets.Scripts.Craft.Parts { public class PartScript { public AircraftScript Aircraft; } }
namespace Assets.Scripts.Craft.Parts.Modifiers
{
    public class CameraVantageData { public bool LookAtCockpit; }
    public class CameraVantageScript { public CameraVantageData Data = new CameraVantageData(); public PartScript PartScript = new PartScript(); }
    public class SeatScript { public PartScript PartScript = new PartScript(); }
}
namespace Assets.Scripts.Flight.Combat
{
    public class Target { public bool IsDead; public string Name="target"; public Vector3 Position; }
    public class GroundTarget : Target { }
}
namespace Assets.Scripts.Flight.WorldObjects.Vehicles.Land
{
    public class SimpleGroundVehicleScript { public Assets.Scripts.Flight.Combat.GroundTarget Target; }
}
namespace Assets.Scripts.Flight.WorldObjects.Vehicles.Sea
{
    public class SinkableShipScript { public Assets.Scripts.Flight.Combat.GroundTarget Target; }
}
namespace Assets.Scripts.Multiplayer.ActivityFramework.Activities.MechInvasion
{
    public class MechScript { public Assets.Scripts.Flight.Combat.GroundTarget Target; }
}
namespace Assets.Scripts.Flight
{
    public static class PauseManager { public static bool Paused; }
    public class FlightScenePlayer
    {
        public AircraftScript Aircraft, CurrentOrPreviousAircraft;
        public GameObject Avatar = new GameObject(); public SeatScript CurrentIKSeat;
    }
    public class TestFlightUi { public TestTargetingUi TargetingSystem = new TestTargetingUi(); }
    public class TestTargetingUi { public AircraftScript Aircraft; }
    public class FlightSceneScript
    {
        public static FlightSceneScript Instance;
        public FlightScenePlayer LocalPlayer = new FlightScenePlayer(); public TestFlightUi FlightUI = new TestFlightUi();
    }
}
namespace Assets.Scripts.Flight.Cameras
{
    public class TestXR { public bool XrCamerasEnabled; }
    public class CameraManagerScript
    {
        public static CameraManagerScript Instance;
        public CameraController Controller; public Camera MainCamera = new Camera();
        public Transform CameraTransform = new Transform(), CameraFocalPosition = new Transform();
        public TestXR XRCameraManager = new TestXR();
    }
    public class CameraController
    {
        public bool IsActive=true, IsSelected=true;
        public CameraManagerScript CameraManager = CameraManagerScript.Instance;
        public CameraVantageScript CameraVantage;
        public Transform CameraTransform { get { return CameraManager.CameraTransform; } }
    }
    public class InteractiveCameraController : CameraController
    {
        protected Vector2 _deltaRotation;
        public Vector2 Angles { get { return _deltaRotation; } set { _deltaRotation=value; } }
        public int RecenterCalls;
        public Vector2 RecenterStart;
        // Model native completion only. Actual DOTween timing is an in-game check.
        public virtual void RecenterView()
        {
            RecenterCalls++;
            RecenterStart=_deltaRotation;
            _deltaRotation=Vector2.zero;
        }
    }
    public class CockpitCameraController : InteractiveCameraController { }
    public class FirstPersonCameraController : InteractiveCameraController
    {
        private bool _lookAtCockpit, _animatingRecenter;
        public bool CachedCockpit { get { return _lookAtCockpit; } set { _lookAtCockpit=value; } }
        public bool Recentering { get { return _animatingRecenter; } set { _animatingRecenter=value; } }
    }
    public class FirstPersonCharacterCameraController : InteractiveCameraController
    {
        private Vector2 _currentRotation;
        private Func<Transform> _targetTransform;
        private bool _animatingRecenter;
        public bool IsCockpitMode;
        public Vector2 CurrentRotation { get { return _currentRotation; } set { _currentRotation=value; } }
        public Func<Transform> Anchor { get { return _targetTransform; } set { _targetTransform=value; } }
        public bool Recentering { get { return _animatingRecenter; } set { _animatingRecenter=value; } }
    }
    public class TargetingPodCameraController : CameraController { }
    public class OrbitCameraController : InteractiveCameraController { }
    public class ChaseCameraController : InteractiveCameraController { }
    public class FlyByCameraController : InteractiveCameraController { }
}
namespace Assets.Scripts.Input.Events
{
    public enum InputButton { Primary, Middle }
    public struct InputEvent { public InputButton InputButton; }
}
namespace SP2FreeCamera
{
    internal class Setting<T> { public T Value; public Setting(T v) { Value=v; } }
    internal class TestLog { public int Warnings; public void LogWarning(string m) { Warnings++; } }
    internal class Plugin
    {
        public static TestLog Log = new TestLog();
        public Setting<bool> FirstPersonFocusEnabled = new Setting<bool>(true);
        public Setting<KeyCode> FocusSelectedTargetKey = new Setting<KeyCode>(KeyCode.Minus);
        public Setting<float> FocusMaximumDistance = new Setting<float>(100000);
    }
    internal class FreeCameraRuntime
    {
        public static FreeCameraRuntime Instance;
        public bool Active, KeyboardAllowed=true, PointerAllowed=true;
        public Plugin Settings = new Plugin();
        public FirstPersonFocus FirstPersonFocus;
        public List<string> Notices = new List<string>();
        public bool CanProcessFirstPersonKeyboardInput() { return !Active && KeyboardAllowed && Application.isFocused && !PauseManager.Paused; }
        public bool CanProcessFirstPersonPointerInput(Vector2 p) { return CanProcessFirstPersonKeyboardInput() && PointerAllowed; }
        public void Notify(string n,bool w) { Notices.Add(n); }
    }
    internal static class Localization { public static string Text(string k) { return k; } }
    internal enum FocusTargetKind { Terrain, Part, Transform, DynamicGroundTarget, GameTarget }
    internal class FocusTarget
    {
        public FocusTargetKind Kind;
        public Vector3 Position;
        public Assets.Scripts.Flight.Combat.Target Target;
        public static FocusTarget CreateTerrain(Vector3 p) { return new FocusTarget { Kind=FocusTargetKind.Terrain, Position=Assets.Scripts.Utility.ConvertFloatingOriginToAbsolutePosition(p) }; }
        public static FocusTarget CreatePart(PartScript p,Collider c,Vector3 pos) { return new FocusTarget { Kind=FocusTargetKind.Part,Position=pos }; }
        public static FocusTarget CreateDynamicGroundTarget(Transform t,Vector3 p,string n) { return new FocusTarget { Kind=FocusTargetKind.DynamicGroundTarget,Position=p }; }
        public static FocusTarget CreateTransform(Transform t,Vector3 p,string n) { return new FocusTarget { Kind=FocusTargetKind.Transform,Position=p }; }
        public static FocusTarget CreateGameTarget(Assets.Scripts.Flight.Combat.Target t) { return new FocusTarget { Kind=FocusTargetKind.GameTarget,Target=t }; }
        public bool TryGetFloatingOriginPosition(out Vector3 p)
        {
            p=Target != null ? Target.Position : Kind==FocusTargetKind.Terrain ? Assets.Scripts.Utility.ConvertAbsoluteToFloatingOriginPosition(Position) : Position;
            return Target==null || !Target.IsDead;
        }
    }
    internal enum ReleasedWeaponKind { Missile, Bomb }
    internal struct ReleasedWeaponSelection { public Transform AnchorTransform; public Vector3 WorldPosition; public ReleasedWeaponKind Kind; }
    internal static class ReleasedWeaponSelectionHelper
    {
        public static int Calls;
        public static bool TryFindNearScreenPoint(Camera c,Vector2 p,float d,float t,float o,out ReleasedWeaponSelection w)
        { Calls++; w=new ReleasedWeaponSelection { AnchorTransform=null, WorldPosition=Vector3.zero, Kind=ReleasedWeaponKind.Missile }; return false; }
    }
}
namespace SP2FreeCamera.Tests
{
    public static class FirstPersonFocusTests
    {
        private static FreeCameraRuntime Runtime;
        private static FirstPersonFocus Focus { get { return Runtime.FirstPersonFocus; } }
        private static CameraManagerScript Manager { get { return CameraManagerScript.Instance; } }
        private static readonly List<string> Passed = new List<string>();
        private static void Require(bool c,string m) { if(!c)throw new Exception(m); }
        private static void Near(Vector3 a,Vector3 b,string m,float epsilon=0.0002f) { Require((a-b).sqrMagnitude<=epsilon*epsilon,m); }
        private static void Rotation(Quaternion a,Quaternion b,string m)
        { Near(a*Vector3.forward,b*Vector3.forward,m+" forward"); Near(a*Vector3.up,b*Vector3.up,m+" up"); }
        private static object Hook(string name,params object[] args)
        { return typeof(FirstPersonFocusPatches).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args); }
        private static T Reset<T>() where T:InteractiveCameraController,new()
        {
            CameraManagerScript.Instance=new CameraManagerScript();
            FlightSceneScript.Instance=new FlightSceneScript();
            var craft=new AircraftScript();
            FlightSceneScript.Instance.LocalPlayer.Aircraft=craft;
            FlightSceneScript.Instance.LocalPlayer.CurrentOrPreviousAircraft=craft;
            FlightSceneScript.Instance.FlightUI.TargetingSystem.Aircraft=craft;
            Runtime=new FreeCameraRuntime(); FreeCameraRuntime.Instance=Runtime;
            Runtime.FirstPersonFocus=new FirstPersonFocus(Runtime);
            T c=new T(); Manager.Controller=c;
            Input.MiddleDown=Input.MiddleHeld=Input.MiddleUp=false;
            Input.DownKey=KeyCode.None;
            Application.isFocused=true; PauseManager.Paused=false; Cursor.lockState=CursorLockMode.None;
            Time.frameCount++; Time.unscaledDeltaTime=1f/60;
            Assets.Scripts.Utility.Origin=Vector3.zero;
            Physics.Hits=new RaycastHit[0]; Physics.Calls=0; ReleasedWeaponSelectionHelper.Calls=0;
            Manager.CameraTransform.position=new Vector3(30,400,-50);
            Manager.CameraFocalPosition.position=new Vector3(8,9,10);
            return c;
        }
        private static void Frame()
        { Time.frameCount++; Input.DownKey=KeyCode.None; Input.MiddleDown=Input.MiddleHeld=Input.MiddleUp=false; }
        private static Assets.Scripts.Flight.Combat.Target Lock(Vector3 direction)
        {
            var target=new Assets.Scripts.Flight.Combat.Target { Position=Manager.CameraTransform.position+direction*100 };
            FlightSceneScript.Instance.FlightUI.TargetingSystem.Aircraft.TargetingSystem.CurrentTarget=target;
            Input.DownKey=KeyCode.Minus; Hook("BeforeNativeInput"); Input.DownKey=KeyCode.None;
            Require(Focus.HasFocus(Manager.Controller),"Main minus should acquire a target outside freecam");
            return target;
        }
        private static void Pose(InteractiveCameraController c,Quaternion basis,Quaternion roll,Vector2 oldAngles)
        {
            c.Angles=oldAngles;
            Hook("CaptureNativeInput",c,oldAngles);
            c.CameraTransform.rotation=basis*FirstPersonFocusMath.LookOffset(oldAngles)*roll;
            // Classic/part camera auto-return happens AFTER the pose write.
            if(!(c is FirstPersonCharacterCameraController))c.Angles=new Vector2(oldAngles.x*0.7f,oldAngles.y*0.7f);
            string hook=c is CockpitCameraController ? "AfterCockpitPose" : c is FirstPersonCameraController ? "AfterCameraPose" : "AfterCharacterPose";
            Hook(hook,c);
        }
        private static RaycastHit Hit(int layer,float distance,PartScript part=null,object ground=null,Transform parent=null)
        {
            var collider=new Collider(); collider.gameObject.layer=layer; collider.transform.parent=parent;
            if(part!=null)collider.Components[typeof(PartScript)]=part;
            if(ground!=null)collider.Components[ground.GetType()]=ground;
            return new RaycastHit { collider=collider,distance=distance,point=new Vector3(0,0,distance) };
        }
        public static string[] RunAll()
        {
            Passed.Clear();
            foreach(float bank in new[]{-180f,-95f,-30f,0f,45f,120f,179f})
            foreach(float pitch in new[]{-65f,0f,53f})
            foreach(float yaw in new[]{-160f,-42f,0f,95f,170f})
            foreach(float headRoll in new[]{-75f,0f,27f})
            {
                Quaternion basis=Quaternion.Euler(17,36,bank), roll=Quaternion.Euler(0,0,headRoll);
                Vector2 desired=new Vector2(pitch,yaw);
                Quaternion expected=basis*FirstPersonFocusMath.LookOffset(desired)*roll;
                Vector2 actualAngles; Quaternion actual;
                Require(FirstPersonFocusMath.TrySolve(expected*Vector3.forward,basis,roll,0,out actualAngles,out actual),"Valid orientation");
                Rotation(actual,expected,"Native bank and local roll must survive arbitrary target look");
            }
            Passed.Add("105 mounted orientations x 3 head-roll corrections preserve native roll and exact aim");

            Vector2 angle; Quaternion rotation;
            foreach(float pole in new[]{-1f,1f})
            {
                Quaternion basis=Quaternion.Euler(25,47,113);
                Require(FirstPersonFocusMath.TrySolve(basis*(Vector3.up*pole),basis,Quaternion.identity,123,out angle,out rotation),"Pole valid");
                Require(Math.Abs(angle.y-123)<0.001f,"Pole retains local yaw");
                Near(rotation*Vector3.forward,basis*(Vector3.up*pole),"Exact pole aim");
            }
            foreach(Vector3 invalid in new[]{Vector3.zero,new Vector3(float.NaN,1,0),new Vector3(float.PositiveInfinity,0,1)})
                Require(!FirstPersonFocusMath.TrySolve(invalid,Quaternion.identity,Quaternion.identity,0,out angle,out rotation),"Invalid direction rejected");
            Passed.Add("Vertical targets retain local yaw; coincident and non-finite directions are rejected safely");

            foreach(bool partCamera in new[]{false,true})
            foreach(float bank in new[]{0f,70f,180f,260f})
            {
                InteractiveCameraController c=partCamera ? (InteractiveCameraController)Reset<FirstPersonCameraController>() : Reset<CockpitCameraController>();
                Quaternion basis=Quaternion.Euler(26,-61,bank), expected=basis*FirstPersonFocusMath.LookOffset(new Vector2(-31,105));
                Vector3 position=Manager.CameraTransform.position,focal=Manager.CameraFocalPosition.position;
                Lock(expected*Vector3.forward);
                Pose(c,basis,Quaternion.identity,new Vector2(24,-42));
                Rotation(c.CameraTransform.rotation,expected,"Native reference extracted before auto-center mutation");
                Near(c.CameraTransform.position,position,"No position write"); Near(Manager.CameraFocalPosition.position,focal,"No focal write");
                Require(Manager.MainCamera.fieldOfView==51,"No FOV write");
            }
            Passed.Add("Classic cockpit and part cameras preserve mounting/AutoOrient reference despite native auto-centering; position/focal/FOV untouched");

            var seated=Reset<FirstPersonCharacterCameraController>(); seated.IsCockpitMode=true;
            var anchor=new Transform(); seated.Anchor=()=>anchor;
            var moving=Lock(new Vector3(1,0.3f,2).normalized);
            for(int frame=0;frame<180;frame++)
            {
                Frame();
                anchor.rotation=Quaternion.Euler(20,frame*2,frame*4);
                Quaternion roll=Quaternion.Euler(0,0,(float)Math.Sin(frame*0.08)*60);
                Vector2 aim=new Vector2(-35,55);
                Quaternion expected=anchor.rotation*FirstPersonFocusMath.LookOffset(aim)*roll;
                // Multiple native updates in the SAME rendered frame, with fresh
                // camera/target poses (physics, LateUpdate and seat IK).
                for(int call=0;call<3;call++)
                {
                    seated.CameraTransform.position=new Vector3(frame*2+call,400,-50);
                    moving.Position=seated.CameraTransform.position+(expected*Vector3.forward)*(100+call*40);
                    Pose(seated,anchor.rotation,roll,new Vector2(16,-23));
                    Rotation(seated.CameraTransform.rotation,expected,"Fresh native Chicken Head correction every update");
                }
            }
            Passed.Add("Seated FPV reuses changing native Chicken Head correction through 540 physics/render/IK pose updates without frame gating or lag");

            var foot=Reset<FirstPersonCharacterCameraController>();
            Quaternion footLook=FirstPersonFocusMath.LookOffset(new Vector2(-35,72));
            Lock(footLook*Vector3.forward);
            Pose(foot,Quaternion.identity,Quaternion.identity,new Vector2(12,20));
            Rotation(foot.CameraTransform.rotation,footLook,"Native on-foot world-relative policy");
            Frame();
            Require(Focus.BeforeNativeRotate(foot,new Vector2(0,1)),"Manual look handed back");
            Require(!Focus.HasFocus(foot),"Manual rotation releases lock");
            Require(Math.Abs(foot.Angles.y-72)<0.01 && Math.Abs(foot.CurrentRotation.y-72)<0.01,"Both native look caches start at the locked direction");
            Passed.Add("On-foot FPV preserves its native policy; manual release seeds both target and smoothed native look caches");

            var cockpit=Reset<CockpitCameraController>(); Lock(Vector3.forward);
            Require(!Focus.BeforeNativeRotate(cockpit,new Vector2(1,1)),"Same-click motion must not cancel");
            Frame(); Require(Focus.BeforeNativeRotate(cockpit,Vector2.zero) && Focus.HasFocus(cockpit),"Zero mouse delta retains lock");
            Runtime.KeyboardAllowed=false;
            Require(!Focus.BeforeNativeRotate(cockpit,new Vector2(1,1)) && Focus.HasFocus(cockpit),"UI interaction must not release behind menu");
            Runtime.KeyboardAllowed=true;
            object[] available={cockpit,false}; Hook("CockpitRecenterAvailable",available);
            Require((bool)available[1],"Native recenter available during lock");
            Hook("RecenterCockpit",cockpit); Require(!Focus.HasFocus(cockpit),"Recenter releases lock");
            Passed.Add("Acquisition-frame movement, zero input, UI blocking and native recenter have explicit non-conflicting ownership");

            foreach(CameraController excluded in new CameraController[]{new TargetingPodCameraController(),new OrbitCameraController(),new ChaseCameraController(),new FlyByCameraController(),new CameraController()})
                Require(!FirstPersonFocus.IsSupportedType(excluded),"Explicit whitelist excludes "+excluded.GetType().Name);
            foreach(int guard in new[]{0,1,2,3,4,5,6,7,8})
            {
                var camera=Reset<FirstPersonCameraController>(); camera.CameraVantage=new CameraVantageScript();
                if(guard==0)Runtime.Active=true;
                if(guard==1)Runtime.Settings.FirstPersonFocusEnabled.Value=false;
                if(guard==2)Manager.XRCameraManager.XrCamerasEnabled=true;
                if(guard==3)camera.CachedCockpit=true;
                if(guard==4)camera.CameraVantage.Data.LookAtCockpit=true;
                if(guard==5)camera.IsSelected=false;
                if(guard==6)camera.IsActive=false;
                if(guard==7)camera.Recentering=true;
                if(guard==8)Manager.Controller=new TargetingPodCameraController();
                Input.MiddleDown=Input.MiddleHeld=true; Focus.ProcessFrame();
                Require(Physics.Calls==0,"Unsupported/inactive/native-priority view must not pick");
                Require(!Focus.ConsumeMiddle(camera),"Native middle interaction remains untouched");
            }
            Passed.Add("Pods/orbit/chase/flyby/freecam, VR, disabled/unselected cameras and both cached/live Look At Cockpit flags stay outside the module");

            var normal=Reset<FirstPersonCameraController>(); normal.CameraVantage=new CameraVantageScript();
            Lock(Vector3.forward); normal.CachedCockpit=true; Focus.ProcessFrame();
            Require(!Focus.HasFocus(normal),"Enabling native Look At Cockpit clears lock immediately");
            normal.CachedCockpit=false; Frame(); Focus.ProcessFrame();
            Require(!Focus.HasFocus(normal),"No surprise relock after cockpit priority ends");
            Passed.Add("Look At Cockpit takes over an existing lock and disabling it does not silently rearm");

            cockpit=Reset<CockpitCameraController>();
            var aircraft=FlightSceneScript.Instance.LocalPlayer.Aircraft;
            var ownPart=new PartScript { Aircraft=aircraft };
            var otherPart=new PartScript { Aircraft=new AircraftScript() };
            var terrain=Hit(Assets.Scripts.Layers.TerrainLayer,300);
            var other=Hit(Assets.Scripts.Layers.RemoteAircraftLayer,100,otherPart);
            Physics.Hits=new[]{terrain,other,Hit(Assets.Scripts.Layers.AircraftLayer,1,ownPart),
                Hit(Assets.Scripts.Layers.AircraftLayer,2,otherPart,parent:FlightSceneScript.Instance.LocalPlayer.Avatar.transform)};
            FocusTarget picked=FocusSelection.Pick(Manager.MainCamera,new Ray(),new Vector2(),1000,
                aircraft,FlightSceneScript.Instance.LocalPlayer.Avatar.transform,false);
            Require(picked.Kind==FocusTargetKind.Part && picked.Position.z==100,"First-person must skip local cabin/avatar without skipping remote parts");
            picked=FocusSelection.Pick(Manager.MainCamera,new Ray(),new Vector2(),1000);
            Require(picked.Position.z==1 && ReleasedWeaponSelectionHelper.Calls==1,"Freecam keeps own-part picking and released-weapon fallback");
            var car=new Assets.Scripts.Flight.WorldObjects.Vehicles.Land.SimpleGroundVehicleScript {
                Target=new Assets.Scripts.Flight.Combat.GroundTarget() };
            Physics.Hits=new[]{other,terrain,Hit(Assets.Scripts.Layers.CarLayer,50,ground:car)};
            picked=FocusSelection.Pick(Manager.MainCamera,new Ray(),new Vector2(),1000,includeReleasedWeapons:false);
            Require(picked.Kind==FocusTargetKind.DynamicGroundTarget,"Nearer moving ground target wins");
            Physics.Hits=new[]{Hit(Assets.Scripts.Layers.TerrainLayer,30),other};
            picked=FocusSelection.Pick(Manager.MainCamera,new Ray(),new Vector2(),1000,includeReleasedWeapons:false);
            Require(picked.Kind==FocusTargetKind.Terrain,"Terrain occludes more distant selectable parts");
            var trigger=Hit(Assets.Scripts.Layers.AircraftLayer,1,otherPart); trigger.collider.isTrigger=true;
            Physics.Hits=new[]{trigger,terrain};
            picked=FocusSelection.Pick(Manager.MainCamera,new Ray(),new Vector2(),1000,includeReleasedWeapons:false);
            Require(picked.Kind==FocusTargetKind.Terrain,"Unrelated triggers excluded");
            trigger.collider.gameObject.layer=Assets.Scripts.Layers.AircraftInteractable;
            picked=FocusSelection.Pick(Manager.MainCamera,new Ray(),new Vector2(),1000,includeReleasedWeapons:false);
            Require(picked.Kind==FocusTargetKind.Part,"Aircraft interactable triggers remain selectable");
            Passed.Add("Shared picker retains nearest part/ground/terrain and trigger rules; only first-person excludes local cabin/avatar and released-weapon fallback");

            foreach(bool lockedCursor in new[]{false,true})
            {
                cockpit=Reset<CockpitCameraController>();
                Manager.MainCamera.pixelRect=new Rect(100,20,800,600);
                Input.mousePosition=new Vector3(300,200,0);
                Cursor.lockState=lockedCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Physics.Hits=new[]{terrain}; Input.MiddleDown=Input.MiddleHeld=true;
                bool consumed=(bool)Hook("BeforeNativePointer",cockpit,
                    new Assets.Scripts.Input.Events.InputEvent { InputButton=Assets.Scripts.Input.Events.InputButton.Middle });
                Focus.ProcessFrame(); Hook("BeforeNativeInput");
                Require(!consumed && Physics.Calls==1 && Focus.HasFocus(cockpit),"Native-before-plugin ordering picks once and consumes middle");
                Vector2 expected=lockedCursor ? Manager.MainCamera.pixelRect.center : new Vector2(300,200);
                Require(Manager.MainCamera.LastRayPosition.x==expected.x && Manager.MainCamera.LastRayPosition.y==expected.y,"Screen point follows cursor ownership and viewport");
                Frame(); Input.MiddleHeld=true; Runtime.PointerAllowed=false; Focus.ProcessFrame();
                Require(Focus.ConsumeMiddle(cockpit),"Captured native middle drag must not resume while crossing UI");
                Frame(); Input.MiddleUp=true; Focus.ProcessFrame();
                Require(Focus.ConsumeMiddle(cockpit),"Consume the release event too");
                Frame(); Focus.ProcessFrame(); Require(!Focus.ConsumeMiddle(cockpit),"Release ends middle ownership on next frame");
                Require(ReleasedWeaponSelectionHelper.Calls==0,"No targeting pod/released weapon selection extras");
            }
            Passed.Add("Middle pick runs once with either input-update order; locked cursor uses viewport center and capture lasts through native drag/end");

            foreach(int guard in new[]{0,1,2,3})
            {
                cockpit=Reset<CockpitCameraController>();
                if(guard==0)Runtime.PointerAllowed=false;
                if(guard==1)Runtime.KeyboardAllowed=false;
                if(guard==2)Application.isFocused=false;
                if(guard==3)PauseManager.Paused=true;
                Input.MiddleDown=Input.MiddleHeld=true; Focus.ProcessFrame();
                Require(Physics.Calls==0 && !Focus.ConsumeMiddle(cockpit),"Input guards do not steal a native middle sequence");
            }
            Passed.Add("UI, keyboard/chat, application focus and pause gates prevent acquisition and native middle capture");

            cockpit=Reset<CockpitCameraController>();
            var originalTarget=Lock(new Vector3(1,0,2).normalized);
            Frame();
            FlightSceneScript.Instance.FlightUI.TargetingSystem.Aircraft.TargetingSystem.CurrentTarget=
                new Assets.Scripts.Flight.Combat.Target { Position=new Vector3(-800,20,0) };
            Pose(cockpit,Quaternion.identity,Quaternion.identity,Vector2.zero);
            Near(cockpit.CameraTransform.rotation*Vector3.forward,(originalTarget.Position-cockpit.CameraTransform.position).normalized,"Lock snapshots target identity, not target coordinates");
            originalTarget.Position=new Vector3(900,400,20);
            Pose(cockpit,Quaternion.identity,Quaternion.identity,Vector2.zero);
            Near(cockpit.CameraTransform.rotation*Vector3.forward,(originalTarget.Position-cockpit.CameraTransform.position).normalized,"Original moving target read again in the same rendered frame");
            Passed.Add("Minus keeps the selected target identity while sampling its fresh position on every native pose update");

            originalTarget.IsDead=true; Frame();
            Pose(cockpit,Quaternion.identity,Quaternion.identity,Vector2.zero);
            for(int i=0;i<100;i++)Pose(cockpit,Quaternion.identity,Quaternion.identity,Vector2.zero);
            Require(Focus.HasFocus(cockpit),"Multiple physical/IK updates must not multiply the loss timer");
            Vector3 originShift=new Vector3(1000,0,-500);
            Assets.Scripts.Utility.Origin=originShift; cockpit.CameraTransform.position-=originShift;
            Pose(cockpit,Quaternion.identity,Quaternion.identity,Vector2.zero);
            Near(cockpit.CameraTransform.rotation*Vector3.forward,
                (originalTarget.Position-originShift-cockpit.CameraTransform.position).normalized,"Lost-target hold uses absolute position across floating-origin shifts");
            Application.isFocused=false;
            for(int i=0;i<30;i++){Frame();Pose(cockpit,Quaternion.identity,Quaternion.identity,Vector2.zero);}
            Require(Focus.HasFocus(cockpit),"Loss timer pauses while unfocused");
            Application.isFocused=true;
            for(int i=0;i<20;i++){Frame();Pose(cockpit,Quaternion.identity,Quaternion.identity,Vector2.zero);}
            Require(!Focus.HasFocus(cockpit),"Genuinely lost target releases after grace with native handoff");
            Passed.Add("Lost-target grace advances once per rendered frame, pauses when unfocused and remains floating-origin safe");

            cockpit=Reset<CockpitCameraController>(); Lock(Vector3.forward);
            object[] switchState={Manager,null}; Hook("BeforeCameraSwitch",switchState);
            Hook("AfterCameraSwitch",Manager,switchState[1]);
            Require(Focus.HasFocus(cockpit),"Rejected/no-op native camera switch keeps focus");
            Manager.Controller=new OrbitCameraController(); Hook("AfterCameraSwitch",Manager,switchState[1]);
            Manager.Controller=cockpit; Frame(); Focus.ProcessFrame();
            Require(!Focus.HasFocus(cockpit),"Actual camera switch clears state and does not rearm on return");
            Frame(); Input.DownKey=KeyCode.KeypadMinus; Focus.ProcessFrame();
            Require(!Focus.HasFocus(cockpit),"Numpad minus is not the default shortcut");
            Frame(); Runtime.Settings.FocusSelectedTargetKey.Value=KeyCode.None; Input.DownKey=KeyCode.Minus; Focus.ProcessFrame();
            Require(!Focus.HasFocus(cockpit),"Unbound shortcut is disabled");
            Passed.Add("Camera switches clear independent focus state only after a real switch; Numpad minus/None do not acquire");

            foreach(int kind in new[]{0,1,2,3})
            foreach(bool locked in new[]{false,true})
            {
                InteractiveCameraController camera=kind==0 ? (InteractiveCameraController)Reset<CockpitCameraController>() :
                    kind==1 ? (InteractiveCameraController)Reset<FirstPersonCameraController>() : Reset<FirstPersonCharacterCameraController>();
                var character=camera as FirstPersonCharacterCameraController;
                Quaternion basis=kind==2 ? Quaternion.identity : Quaternion.Euler(18,-50,125);
                Quaternion roll=kind==3 ? Quaternion.Euler(0,0,-25) : Quaternion.identity;
                if(character!=null)
                {
                    character.IsCockpitMode=kind==3;
                    character.Anchor=()=>new Transform { rotation=basis };
                }
                Vector2 desired=new Vector2(-24,75);
                camera.Angles=desired;
                if(locked)
                {
                    Lock((basis*FirstPersonFocusMath.LookOffset(desired)*roll)*Vector3.forward);
                    Pose(camera,basis,roll,new Vector2(15,-20));
                }
                Vector3 position=Manager.CameraTransform.position,focal=Manager.CameraFocalPosition.position;
                Quaternion before=camera.CameraTransform.rotation;
                Frame(); Input.DownKey=KeyCode.Backspace;
                Hook("BeforeNativeInput"); Focus.ProcessFrame(); Hook("BeforeNativeInput");
                Require(camera.RecenterCalls==1,"Backspace calls native recenter once in each supported first-person camera");
                Require(!Focus.HasFocus(camera),"Backspace releases any target before native recenter");
                if(locked)Require(Math.Abs(camera.RecenterStart.y-desired.y)<0.01f,"Native recenter starts from the locked direction");
                Require(camera.Angles.sqrMagnitude==0,"Native recenter completion resets local look");
                Rotation(camera.CameraTransform.rotation,before,"Hotkey must not write the camera's roll/rotation directly");
                Near(Manager.CameraTransform.position,position,"Recenter hotkey leaves position native");
                Near(Manager.CameraFocalPosition.position,focal,"Recenter hotkey leaves focal position native");
                Require(Manager.MainCamera.fieldOfView==51,"Recenter hotkey leaves FOV native");
                Pose(camera,basis,roll,Vector2.zero);
                Rotation(camera.CameraTransform.rotation,basis*roll,"Recentered view preserves native mounting/Chicken Head roll");
                Frame(); Focus.ProcessFrame();
                Require(camera.RecenterCalls==1,"A held key without another key-down must not restart recenter");
            }
            Passed.Add("Backspace recenters all supported first-person cameras with or without a lock, hands off locked angles once and preserves native position/FOV/roll");

            foreach(bool cached in new[]{false,true})
            {
                var camera=Reset<FirstPersonCameraController>(); camera.CameraVantage=new CameraVantageScript();
                camera.CachedCockpit=cached; camera.CameraVantage.Data.LookAtCockpit=!cached;
                camera.Angles=new Vector2(20,35);
                camera.CameraTransform.rotation=Quaternion.Euler(15,63,127);
                Quaternion original=camera.CameraTransform.rotation;
                Input.DownKey=KeyCode.Backspace; Input.MiddleDown=Input.MiddleHeld=true;
                Focus.ProcessFrame();
                Require(camera.RecenterCalls==1 && camera.Angles.sqrMagnitude==0,"Look At Cockpit still allows native local-offset recenter");
                Require(camera.CachedCockpit==cached && camera.CameraVantage.Data.LookAtCockpit==!cached,"Native cockpit priority flags untouched");
                Rotation(camera.CameraTransform.rotation,original,"Look At Cockpit rotation not overridden");
                Require(Physics.Calls==0 && !Focus.ConsumeMiddle(camera),"Recenter does not enable target picking under native cockpit priority");
            }
            Passed.Add("Backspace delegates to native local-offset recenter under both cached/live Look At Cockpit without overriding its target, rotation or middle input");

            foreach(int guard in new[]{0,1,2,3,4,5,6,7,8,9,10,11})
            {
                var camera=Reset<FirstPersonCameraController>();
                if(guard==0)Runtime.Active=true;
                if(guard==1)Runtime.Settings.FirstPersonFocusEnabled.Value=false;
                if(guard==2)Manager.XRCameraManager.XrCamerasEnabled=true;
                if(guard==3)Runtime.KeyboardAllowed=false;
                if(guard==4)Application.isFocused=false;
                if(guard==5)PauseManager.Paused=true;
                if(guard==6)camera.Recentering=true;
                if(guard==7)Manager.Controller=new TargetingPodCameraController();
                if(guard==8)Manager.Controller=new OrbitCameraController();
                if(guard==9)Manager.Controller=new ChaseCameraController();
                if(guard==10)camera.IsSelected=false;
                if(guard==11)camera.IsActive=false;
                Input.DownKey=KeyCode.Backspace; Focus.ProcessFrame();
                Require(camera.RecenterCalls==0,"Recenter respects supported-view/input/animation guards");
                var current=Manager.Controller as InteractiveCameraController;
                Require(current==null || current.RecenterCalls==0,"No recenter routed to an unsupported current camera");
            }
            Passed.Add("Backspace respects freecam/pod/third-person/VR exclusions, input protection and in-progress recenter without stacking tweens");

            cockpit=Reset<CockpitCameraController>();
            Physics.Hits=new[]{terrain};
            Runtime.Settings.FocusSelectedTargetKey.Value=KeyCode.Backspace;
            FlightSceneScript.Instance.FlightUI.TargetingSystem.Aircraft.TargetingSystem.CurrentTarget=
                new Assets.Scripts.Flight.Combat.Target { Position=new Vector3(100,100,100) };
            Input.DownKey=KeyCode.Backspace; Input.MiddleDown=Input.MiddleHeld=true; Focus.ProcessFrame();
            Require(cockpit.RecenterCalls==1 && Physics.Calls==0 && !Focus.HasFocus(cockpit),"Backspace wins over same-frame middle/target acquisition");
            Passed.Add("Backspace has priority over same-frame middle-click or a conflicting target key, so recenter cannot immediately relock");

            int hooks=0;
            foreach(MethodInfo method in typeof(FirstPersonFocusPatches).GetMethods(BindingFlags.Static|BindingFlags.NonPublic))
            {
                var attributes=(HarmonyLib.HarmonyPatch[])method.GetCustomAttributes(typeof(HarmonyLib.HarmonyPatch),false);
                Require(attributes.Length<=1,"One target descriptor per method; multiple HarmonyPatch descriptors are not multiple targets");
                foreach(var patch in attributes)
                {
                    hooks++;
                    Require(patch.type!=typeof(TargetingPodCameraController) && patch.type!=typeof(OrbitCameraController),"No pod/orbit patch targets");
                }
            }
            Require(hooks==15,"All 15 intended native hook entry points are present");
            Require(Plugin.Log.Warnings==0,"No hook silently fell back after an exception");
            Passed.Add("Patch entry points cover all three supported native pose/recenter paths and exclude targeting pod/orbit methods");
            return Passed.ToArray();
        }
    }
}
