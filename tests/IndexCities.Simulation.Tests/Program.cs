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
                var rows=method.GetCustomAttribute<TheoryAttribute>()!=null
                    ?method.GetCustomAttributes<InlineDataAttribute>().SelectMany(data=>data.GetData(method)).ToArray()
                    :new[]{Array.Empty<object>()};
                if(rows.Length==0){failed++;Console.Error.WriteLine("FAIL "+type.Name+"."+method.Name+": executor local exige InlineData para Theory");continue;}
                for(var row=0;row<rows.Length;row++){
                    var name=type.Name+"."+method.Name+(rows.Length>1?" ["+(row+1)+"]":"");
                    try{method.Invoke(Activator.CreateInstance(type),rows[row]);passed++;Console.WriteLine("PASS "+name);}
                    catch(Exception ex){failed++;Console.Error.WriteLine("FAIL "+name+"\n"+(ex.InnerException??ex));}
                }
            }
        Console.WriteLine($"Assertions locais: {passed} passaram, {failed} falharam");return failed==0&&passed>0?0:1;
    }
}
