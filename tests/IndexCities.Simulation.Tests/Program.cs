using System.Reflection;
using Xunit;
namespace IndexCities.Simulation.Tests;
internal static class Program
{
    static int Main(string[] args)
    {
        if(args.FirstOrDefault()?.StartsWith("--soak-days=")==true)return MonthlySoak.Run(args[0]);
        // Executa as mesmas assertions quando o testhost do Windows não consegue
        // observar o processo pai. Não substitui discovery/runner do dotnet test.
        var filter=args.FirstOrDefault()??"";var failed=0;var passed=0;
        foreach(var type in Assembly.GetExecutingAssembly().GetTypes().Where(t=>t.IsClass&&!t.IsAbstract))
            foreach(var method in type.GetMethods().Where(m=>m.GetCustomAttribute<FactAttribute>()!=null&&(type.Name+"."+m.Name).Contains(filter,StringComparison.OrdinalIgnoreCase))){
                try{method.Invoke(Activator.CreateInstance(type),null);passed++;Console.WriteLine("PASS "+type.Name+"."+method.Name);}
                catch(Exception ex){failed++;Console.Error.WriteLine("FAIL "+type.Name+"."+method.Name+"\n"+(ex.InnerException??ex));}
            }
        Console.WriteLine($"Assertions locais: {passed} passaram, {failed} falharam");return failed==0&&passed>0?0:1;
    }
}
