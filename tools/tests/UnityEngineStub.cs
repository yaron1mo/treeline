// Compile-only stand-in for the parts of UnityEngine used by unity/Assets/Scripts/Game, so the Game scripts can be
// checked for typos outside Unity (tools/tests/check_game.sh). Signatures copy Unity's; nothing here runs.
// Unity itself remains the real check.
#pragma warning disable 0067, 0649, 0169
using System;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static T FindFirstObjectByType<T>() where T : Object => null;
        public static void Destroy(Object o) { }
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);
    }
    public class Component : Object
    {
        public Transform transform => null;
        public GameObject gameObject => null;
        public T GetComponent<T>() => default(T);
    }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour { }
    public enum SendMessageOptions { RequireReceiver, DontRequireReceiver }
    public sealed class GameObject : Object
    {
        public GameObject(string name) { }
        public string tag;
        public static GameObject Find(string name) => null;
        public void SendMessage(string method, object value, SendMessageOptions options) { }
        public T AddComponent<T>() where T : Component => null;
        public Transform transform => null;
    }
    public class Transform : Component
    {
        public Vector3 position, localPosition, localScale;
        public Quaternion rotation;
        public void SetParent(Transform p, bool worldPositionStays) { }
    }
    public sealed class SpriteRenderer : Component { public Sprite sprite; public Color color; public int sortingOrder; public bool enabled; }
    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float pixelsPerUnit) => null;
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType) => null;
    }
    public enum SpriteMeshType { Tight, FullRect }
    public class Texture : Object { public FilterMode filterMode; public int width => 0; public int height => 0; }
    public sealed class Texture2D : Texture
    {
        public Texture2D(int w, int h, TextureFormat f, bool mipChain) { }
        public static Texture2D whiteTexture => null;
        public TextureWrapMode wrapMode;
        public void SetPixels32(Color32[] c) { }
        public void Apply() { }
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
    }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point, Bilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum CameraClearFlags { Skybox, SolidColor }
    public sealed class Camera : Behaviour
    {
        public static Camera main => null;
        public bool orthographic;
        public float orthographicSize;
        public Color backgroundColor;
        public CameraClearFlags clearFlags;
        public Rect rect;
        public float aspect;
        public Vector3 ScreenToWorldPoint(Vector3 p) => p;
        public Vector3 WorldToScreenPoint(Vector3 p) => p;
    }
    public class TextAsset : Object { public string text => ""; }
    public static class Resources
    {
        public static T[] LoadAll<T>(string path) where T : Object => new T[0];
        public static T Load<T>(string path) where T : Object => null;
    }
    public static class Application { public static string dataPath = ""; public static string persistentDataPath = ""; }
    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
        public static void LogError(object m) { }
    }
    public static class Screen { public static int width, height; }
    public static class Time { public static float deltaTime, time, unscaledDeltaTime; public static int frameCount; }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public Vector2 normalized => this;
        public static Vector2 zero => default(Vector2);
        public static Vector2 right => new Vector2(1f, 0f);
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public static float Dot(Vector2 a, Vector2 b) => 0f;
        public static float Distance(Vector2 a, Vector2 b) => 0f;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a;
        public static Vector2 operator -(Vector2 a, Vector2 b) => a;
        public static Vector2 operator -(Vector2 a) => a;
        public static Vector2 operator +(Vector2 a, Vector2 b) => a;
        public static Vector2 operator *(Vector2 a, float d) => a;
        public static Vector2 operator *(float d, Vector2 a) => a;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default(Vector3);
        public static Vector3 one => new Vector3(1f, 1f, 1f);
        public static Vector3 operator *(Vector3 a, float b) => a;
    }
    public struct Quaternion
    {
        public static Quaternion Euler(float x, float y, float z) => default(Quaternion);
    }
    public struct Rect { public float x, y, width, height; public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; } }
    public struct Matrix4x4 { public static Matrix4x4 Scale(Vector3 v) => default(Matrix4x4); public static Matrix4x4 identity => default(Matrix4x4); }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => default(Color);
        public static Color black => default(Color);
        public static Color Lerp(Color a, Color b, float t) => a;
        public static implicit operator Color32(Color c) => default(Color32);
        public static implicit operator Color(Color32 c) => default(Color);
    }
    public struct Color32 { public byte r, g, b, a; public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; } }
    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public static float SmoothStep(float a, float b, float t) => t;
        public static float Pow(float a, float p) => a;
        public static float Atan2(float a, float b) => a;
        public static float Repeat(float t, float length) => t;
        public static float Abs(float v) => v;
        public static float Lerp(float a, float b, float t) => a;
        public static float Max(float a, float b) => a;
        public static int Max(int a, int b) => a;
        public static float Min(float a, float b) => a;
        public static int Min(int a, int b) => a;
        public static float Clamp01(float v) => v;
        public static float Clamp(float v, float a, float b) => v;
        public static int Clamp(int v, int a, int b) => v;
        public static float Sqrt(float v) => v;
        public static float Sin(float v) => v;
        public static float Cos(float v) => v;
        public static int CeilToInt(float v) => 0;
        public static int RoundToInt(float v) => 0;
        public static int FloorToInt(float v) => 0;
    }
    public enum EventType { MouseDown, MouseUp, Repaint, Layout, Used }
    public sealed class Event { public static Event current => null; public EventType type; public int button; public Vector2 mousePosition; public void Use() { } }
    public enum FontStyle { Normal, Bold }
    public enum TextAnchor { MiddleCenter, UpperLeft }
    public sealed class GUIStyleState { public Color textColor; }
    public sealed class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public int fontSize;
        public bool wordWrap;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public GUIStyleState normal = new GUIStyleState();
        public static implicit operator GUIStyle(string name) => null;
    }
    public sealed class GUISkin { public GUIStyle label, button, box; }
    public sealed class GUILayoutOption { }
    public static class GUI
    {
        public static Matrix4x4 matrix;
        public static Color color;
        public static bool enabled;
        public static GUISkin skin => null;
        public static void Label(Rect r, string t) { }
        public static void Label(Rect r, string t, GUIStyle s) { }
        public static void DrawTexture(Rect r, Texture t) { }
        public static bool Button(Rect r, string t) => false;
    }
    public static class GUILayout
    {
        public static void BeginArea(Rect r) { }
        public static void EndArea() { }
        public static void BeginHorizontal(params GUILayoutOption[] o) { }
        public static void EndHorizontal() { }
        public static void BeginVertical(params GUILayoutOption[] o) { }
        public static void BeginVertical(GUIStyle s, params GUILayoutOption[] o) { }
        public static void EndVertical() { }
        public static Vector2 BeginScrollView(Vector2 p, params GUILayoutOption[] o) => p;
        public static void EndScrollView() { }
        public static void Label(string t, params GUILayoutOption[] o) { }
        public static void Label(string t, GUIStyle s, params GUILayoutOption[] o) { }
        public static bool Button(string t, params GUILayoutOption[] o) => false;
        public static bool Toggle(bool v, string t, GUIStyle s, params GUILayoutOption[] o) => v;
        public static void Space(float px) { }
        public static GUILayoutOption Height(float h) => null;
        public static GUILayoutOption Width(float w) => null;
    }
}
