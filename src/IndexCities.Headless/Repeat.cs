using System.Text.RegularExpressions;
using IndexCities.Simulation;

namespace IndexCities.Headless;

// Measures how much each SIM's day repeats the previous one: same personal
// events at the same clock times, and same money movements. Only a measurement
// for the research on reusing unchanged days; it does not change the core.
sealed class Repeat
{
    static readonly Regex Digits=new(@"\d+([.,]\d+)?",RegexOptions.Compiled);
    Dictionary<int,List<(long t,string d)>> _hist=new();
    Dictionary<string,List<(long t,string k)>> _money=new();
    readonly Dictionary<string,int> _firstDiff=new();
    public void Day(CitySimulation sim,DateTime from,DateTime to,int day)
    {
        long Clock(DateTime at)=>(long)(at-from).TotalSeconds;
        var hist=new Dictionary<int,List<(long,string)>>();
        foreach(var c in sim.State.Citizens.Where(c=>c.Alive&&c.Resident))
            hist[c.Id]=c.History.Where(e=>e.At>=from&&e.At<to).Select(e=>(Clock(e.At),e.Description)).ToList();
        var money=new Dictionary<string,List<(long,string)>>();
        foreach(var m in sim.State.Ledger.Where(m=>m.At>=from&&m.At<to)){
            var key=$"{m.From}>{m.To}|{m.Category}|{m.Amount}";
            foreach(var account in new[]{m.From,m.To}){if(!money.TryGetValue(account,out var list))money[account]=list=[];list.Add((Clock(m.At),key));}
        }
        if(day>1){
            int n=0,exact=0,near=0,seq=0,ledger=0,ledgerSeq=0,full=0,empty=0;
            foreach(var (id,today) in hist){
                if(!_hist.TryGetValue(id,out var before))continue;n++;
                if(today.Count==0)empty++;
                var sameSeq=today.Count==before.Count&&today.Zip(before).All(p=>Norm(p.First.Item2)==Norm(p.Second.d));
                var sameExact=sameSeq&&today.Zip(before).All(p=>p.First.Item1==p.Second.t&&p.First.Item2==p.Second.d);
                var sameNear=sameSeq&&today.Zip(before).All(p=>Math.Abs(p.First.Item1-p.Second.t)<=300);
                if(sameSeq)seq++;if(sameExact)exact++;if(sameNear)near++;
                var account="sim:"+id;var mt=money.GetValueOrDefault(account)??[];var mb=_money.GetValueOrDefault(account)??[];
                var lSeq=mt.Count==mb.Count&&mt.Zip(mb).All(p=>Norm(p.First.Item2)==Norm(p.Second.k));
                var lExact=mt.Count==mb.Count&&mt.Zip(mb).All(p=>p.First.Item1==p.Second.t&&p.First.Item2==p.Second.k);
                if(lSeq)ledgerSeq++;if(lExact)ledger++;if(sameExact&&lExact)full++;
                if(!sameExact){
                    var i=0;while(i<today.Count&&i<before.Count&&today[i].Item2==before[i].d&&today[i].Item1==before[i].t)i++;
                    string Show(List<(long,string)> l,int k)=>k<l.Count?Norm(l[k].Item2):"(fim do dia)";
                    var reason=i<today.Count&&i<before.Count&&Norm(today[i].Item2)==Norm(before[i].d)?"mesmo evento, outro horário: "+Norm(today[i].Item2):"ontem «"+Show(before.Select(x=>(x.t,x.d)).ToList(),i)+"» hoje «"+Show(today,i)+"»";
                    _firstDiff[reason]=_firstDiff.GetValueOrDefault(reason)+1;
                }
            }
            string P(int a)=>n==0?"-":(100.0*a/n).ToString("F1")+"%";
            Console.WriteLine($"REPEAT day={day} sims={n} historyExact={P(exact)} history±5min={P(near)} historySameOrder={P(seq)} moneyExact={P(ledger)} moneySameOrder={P(ledgerSeq)} fullDayExact={P(full)} emptyDays={P(empty)}");
        }
        _hist=hist.ToDictionary(kv=>kv.Key,kv=>kv.Value.Select(x=>(x.Item1,x.Item2)).ToList());
        _money=money.ToDictionary(kv=>kv.Key,kv=>kv.Value.Select(x=>(x.Item1,x.Item2)).ToList());
    }
    public void Print()
    {
        Console.WriteLine("REPEAT primeiras diferenças mais comuns:");
        foreach(var kv in _firstDiff.OrderByDescending(kv=>kv.Value).Take(15))Console.WriteLine($"  {kv.Value,6}  {kv.Key}");
    }
    static string Norm(string s)=>Digits.Replace(s,"N");
}
