using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

class Program
{
    static void Main()
    {
        string dllPath = @"C:\Users\jossu\.nuget\packages\digitalpersona.dpurunet\3.4.4.127\lib\DPUruNet.dll";
        var resolver = new PathAssemblyResolver(new[] { dllPath });
        using var mlc = new MetadataLoadContext(resolver);
        var asm = mlc.LoadFromAssemblyPath(dllPath);

        foreach (var t in asm.GetTypes().Where(t => t.IsPublic).OrderBy(t => t.FullName))
        {
            Console.WriteLine($"TYPE: {t.Namespace}.{t.Name}");
            foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Console.WriteLine($"  {m.MemberType}: {m.Name}");
                if (m is MethodInfo mi)
                {
                    var ps = string.Join(", ", mi.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Console.WriteLine($"    ({ps}) -> {mi.ReturnType.Name}");
                }
                else if (m is EventInfo ev)
                {
                    var mi2 = ev.GetAddMethod();
                    if (mi2 != null)
                    {
                        var ps = string.Join(", ", mi2.GetParameters().Select(p => p.ParameterType.Name));
                        Console.WriteLine($"    Handler: {ev.EventHandlerType?.Name} ({ps})");
                    }
                }
            }
        }
    }
}
