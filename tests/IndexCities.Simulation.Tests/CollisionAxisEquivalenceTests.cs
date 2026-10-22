using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class CollisionAxisEquivalenceTests
{
    private static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Static;
    private static readonly MethodInfo PenetrationMethod=typeof(CitySimulation).GetMethod("Penetration",Flags)!;
    private static readonly MethodInfo HalfLengthMethod=typeof(CitySimulation).GetMethod("BodyHalfLength",Flags)!;
    private static readonly MethodInfo HalfWidthMethod=typeof(CitySimulation).GetMethod("BodyHalfWidth",Flags)!;

    static double HalfLength(Vehicle v)=>(double)HalfLengthMethod.Invoke(null,[v])!;
    static double HalfWidth(Vehicle v)=>(double)HalfWidthMethod.Invoke(null,[v])!;

    // Copied only into this isolated experiment as an independent numerical
    // oracle. Its allocation and LINQ selector are intentionally retained.
    static double Original(Vehicle a,Point aAt,double aHeading,Vehicle b,Point bAt,double bHeading)
    {
        static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
        var af=new Point(Math.Sin(aHeading),Math.Cos(aHeading));
        var ar=new Point(af.Z,-af.X);
        var bf=new Point(Math.Sin(bHeading),Math.Cos(bHeading));
        var br=new Point(bf.Z,-bf.X);
        var d=bAt-aAt;
        return new[]{af,ar,bf,br}.Min(x=>HalfLength(a)*Math.Abs(Dot(af,x))
            +HalfWidth(a)*Math.Abs(Dot(ar,x))
            +HalfLength(b)*Math.Abs(Dot(bf,x))
            +HalfWidth(b)*Math.Abs(Dot(br,x))-Math.Abs(Dot(d,x)));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(2026)]
    public void OriginalAndAllocationFreePenetrationProduceSameDoubleBits(int seed)
    {
        Assert.NotNull(PenetrationMethod);
        Assert.NotNull(HalfLengthMethod);
        Assert.NotNull(HalfWidthMethod);
        var rng=new Random(seed);
        for(var i=0;i<750;i++)
        {
            Vehicle Vehicle(int id)=>new(){Id=id,PassengerCar=rng.Next(2)==0};
            var a=Vehicle(1);var b=Vehicle(2);
            var at=new Point(rng.NextDouble()*60-30,rng.NextDouble()*60-30);
            var bt=new Point(rng.NextDouble()*60-30,rng.NextDouble()*60-30);
            var ah=(rng.NextDouble()-.5)*Math.Tau;
            var bh=(rng.NextDouble()-.5)*Math.Tau;
            var expected=Original(a,at,ah,b,bt,bh);
            var actual=(double)PenetrationMethod.Invoke(null,[a,at,ah,b,bt,bh])!;
            Assert.Equal(BitConverter.DoubleToInt64Bits(expected),BitConverter.DoubleToInt64Bits(actual));
        }
    }
}
