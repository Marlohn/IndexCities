namespace IndexCities.Simulation;
public static class Prof
{
    public static readonly string[] Names={"MoveVehicles","MoveCitizens","Construction","MaintCond","Infrastructure","OperateBusinesses","UpdateNeeds","EconomyReview","Daily/Monthly/Loans"};
    public static readonly long[] Ticks=new long[9];
    public static long T()=>System.Diagnostics.Stopwatch.GetTimestamp();
    public static long Add(int i,long t){var n=T();Ticks[i]+=n-t;return n;}
    public static void Reset()=>Array.Clear(Ticks);
}
