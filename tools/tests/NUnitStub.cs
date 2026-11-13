// Minimal stand-ins for NUnit and UnityEngine.Application, so the EditMode tests in unity/Assets/Tests/EditMode can be
// compiled and run with Mono outside Unity (tools/tests/run_editmode.sh, CI). Only what the tests use is here; if a
// test uses another Assert method, add it. This does not replace a run in Unity.
using System;
namespace NUnit.Framework {
  public class TestAttribute : Attribute {}
  public static class Assert {
    public static void GreaterOrEqual(float a,float b,string m=null){ if(!(a>=b)) throw new Exception("GreaterOrEqual "+a+" "+b+" "+m);}
    public static void LessOrEqual(float a,float b,string m=null){ if(!(a<=b)) throw new Exception("LessOrEqual "+a+" "+b+" "+m);}
    public static void IsNull(object o,string m=null){ if(o!=null) throw new Exception("IsNull "+m);}
    public static void IsNotNull(object o,string m=null){ if(o==null) throw new Exception("IsNotNull "+m);}
    public static void AreEqual(object e, object a, string m=null){ if(!Equals(e,a)) throw new Exception("AreEqual "+e+" != "+a+" "+m);} 
    public static void AreEqual(float e, float a, float d, string m=null){ if(Math.Abs(e-a)>d) throw new Exception("AreEqual "+e+" != "+a+" "+m);} 
    public static void AreEqual(float e, float a, string m=null){ if(e!=a) throw new Exception("AreEqual "+e+" != "+a+" "+m);} 
    public static void Greater(float a,float b,string m=null){ if(!(a>b)) throw new Exception("Greater "+a+" "+b+" "+m);} 
    public static void Less(float a,float b,string m=null){ if(!(a<b)) throw new Exception("Less "+a+" "+b+" "+m);} 
    public static void IsTrue(bool b,string m=null){ if(!b) throw new Exception("IsTrue "+m);} 
    public static void IsFalse(bool b,string m=null){ if(b) throw new Exception("IsFalse "+m);} 
    public static void IsNotEmpty(System.Collections.ICollection c){ if(c.Count==0) throw new Exception("empty"); }
  }
}
namespace NUnit.Framework { public static class StringAssert { public static void Contains(string e,string a){ if(a==null||!a.Contains(e)) throw new Exception("Contains "+e+" in "+a);} } }
namespace UnityEngine { public static class Application { public static string dataPath = System.IO.Path.GetFullPath(System.Environment.GetEnvironmentVariable("TREELINE_ASSETS") ?? "unity/Assets"); } }
public static class Runner {
  public static int Main(){
    int fail=0, n=0;
    foreach (var t in typeof(Runner).Assembly.GetTypes()) foreach (var m in t.GetMethods()) {
      if (m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false).Length==0) continue;
      n++; try { m.Invoke(Activator.CreateInstance(t), null); if (Environment.GetEnvironmentVariable("TREELINE_VERBOSE") != null) Console.WriteLine("PASS "+t.Name+"."+m.Name); }
      catch (Exception e) { fail++; Console.WriteLine("FAIL "+t.Name+"."+m.Name+": "+(e.InnerException??e).Message); }
    }
    Console.WriteLine(n+" tests, "+fail+" failed"); return fail;
  }
}
