using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

class Program
{
    static void Main()
    {
        string dllPath = @"C:\Users\jossu\.nuget\packages\digitalpersona.dpurunet\3.4.4.127\lib\net461\DPUruNet.dll";
        var alc = new AssemblyLoadContext("inspection", true);
        Assembly asm = alc.LoadFromAssemblyPath(dllPath);

        foreach (var t in asm.GetTypes().OrderBy(x => x.FullName))
        {
            Console.WriteLine($"TYPE: {t.FullName}");
            foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Console.WriteLine($"  {m.MemberType}: {m.Name}");
                if (m is MethodInfo mi)
                {
                    var ps = mi.GetParameters();
                    var paramStr = string.Join(", ", ps.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Console.WriteLine($"    ({paramStr}) -> {mi.ReturnType.Name}");
                }
                else if (m is EventInfo ei)
                {
                    Console.WriteLine($"    HandlerType: {ei.EventHandlerType}");
                }
                else if (m is FieldInfo fi)
                {
                    Console.WriteLine($"    FieldType: {fi.FieldType}");
                }
            }
        }
    }
}
