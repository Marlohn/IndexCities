using System.Reflection;
using System.Text.Json;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class Pr57DeferredSatisfactionTests
{
    static bool Defer(Citizen c,double hunger,double unmet,bool crowded)=>
        (bool)typeof(Citizen).GetMethod("Pr57DeferSatisfaction",BindingFlags.Instance|BindingFlags.NonPublic)!
            .Invoke(c,[hunger,unmet,crowded])!;

    [Fact]
    public void DeferredValuePreservesStepInputsAcrossExternalMutationAndSerialization()
    {
        var c=new Citizen{HungerHours=13,Satisfaction=65};
        Assert.False(Defer(c,13,.123456789,true));
        c.HungerHours=47;
        var expected=Math.Clamp(80-13*.7-.123456789*15-8,0,100);
        Assert.Equal(BitConverter.DoubleToInt64Bits(expected),BitConverter.DoubleToInt64Bits(c.Satisfaction));
        Assert.False(Defer(c,47,.1,false));
        Assert.True(Defer(c,48,.2,true));
        expected=Math.Clamp(80-48*.7-.2*15-8,0,100);
        var json=JsonSerializer.Serialize(c);
        Assert.Equal(expected,JsonDocument.Parse(json).RootElement.GetProperty("Satisfaction").GetDouble());
        var restored=JsonSerializer.Deserialize<Citizen>(json)!;
        Assert.Equal(expected,restored.Satisfaction);
        Defer(c,51,.5,false);
        c.Satisfaction=12.75;
        Assert.Equal(12.75,c.Satisfaction);
    }

    [Fact]
    public void DeferredClampsAndBitwiseFloatingOrderMatchCanonical()
    {
        var c=new Citizen();
        foreach(var (hunger,unmet,crowded) in new (double,double,bool)[]{
            (0,0,false),(70,2,true),(18,.3333333333333,false),(1,-2,true)})
        {
            Defer(c,hunger,unmet,crowded);
            var expected=Math.Clamp(80-hunger*.7-unmet*15-(crowded?8:0),0,100);
            Assert.Equal(BitConverter.DoubleToInt64Bits(expected),BitConverter.DoubleToInt64Bits(c.Satisfaction));
        }
    }
}
