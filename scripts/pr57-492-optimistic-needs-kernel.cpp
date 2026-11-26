// PR57 #492: deliberately optimistic NON-EQUIVALENT needs-only kernel.
// This is a feasibility stress test for per-quantum 10k-citizen throughput,
// NOT a replacement for IndexCities, a causal replay, or a .NET benchmark.
#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <string>
#include <vector>

constexpr int N=10000;
constexpr int STEPS=4000;
constexpr long long TIME_SCALE=1000000LL, POINT_SCALE=1000000000000LL;
struct Row { long long hunger,health; double satisfaction,unmet; };
struct SoA { std::vector<long long> hunger,health; std::vector<double> satisfaction,unmet; };
static inline long long quant(double v) {return (long long)std::round(v*POINT_SCALE);}
static inline double clamp(double v,double lo,double hi) {return std::min(hi,std::max(lo,v));}
static inline void update(long long &hunger,long long &health,double &satisfaction,double unmet){
    hunger+=2*TIME_SCALE;
    double hours=hunger/(3600.0*TIME_SCALE);
    if(hours<18)health=std::min(100*POINT_SCALE,health+quant(2.0/3600*.3));
    health+=quant(-unmet*2.0/86400);
    health=std::clamp(health,0LL,100*POINT_SCALE);
    satisfaction=clamp(80-hours*.7-unmet*15,0,100);
}
static uint64_t mix(uint64_t x){x^=x>>30;x*=0xbf58476d1ce4e5b9ULL;x^=x>>27;x*=0x94d049bb133111ebULL;return x^(x>>31);}
int main(int argc,char **argv){
    if(argc!=2||(std::string(argv[1])!="aos"&&std::string(argv[1])!="soa"))return 2;
    bool soa=std::string(argv[1])=="soa";
    std::vector<Row> rows(N);SoA s;
    s.hunger.resize(N);s.health.resize(N);s.satisfaction.resize(N);s.unmet.resize(N);
    for(int i=0;i<N;i++){
        double unmet=(i%97)*.001+0.2;
        rows[i]={((long long)(i%18000))*TIME_SCALE,90*POINT_SCALE,65,unmet};
        s.hunger[i]=rows[i].hunger;s.health[i]=rows[i].health;s.satisfaction[i]=rows[i].satisfaction;s.unmet[i]=unmet;
    }
    auto begin=std::chrono::steady_clock::now();
    for(int t=0;t<STEPS;t++){
        if(soa){for(int i=0;i<N;i++)update(s.hunger[i],s.health[i],s.satisfaction[i],s.unmet[i]);}
        else {for(int i=0;i<N;i++)update(rows[i].hunger,rows[i].health,rows[i].satisfaction,rows[i].unmet);}
    }
    auto end=std::chrono::steady_clock::now();
    uint64_t check=0;
    for(int i=0;i<N;i++){
        auto h=soa?s.hunger[i]:rows[i].hunger;
        auto v=soa?s.health[i]:rows[i].health;
        auto sat=soa?s.satisfaction[i]:rows[i].satisfaction;
        check+=mix((uint64_t)h)^mix((uint64_t)v)^mix((uint64_t)std::llround(sat*1e6));
    }
    double ms=std::chrono::duration<double,std::milli>(end-begin).count();
    std::printf("PR57_492 mode=%s citizens=%d steps=%d wallMs=%.6f usPerStep=%.6f checksum=%016llx\n",argv[1],N,STEPS,ms,ms*1000/STEPS,(unsigned long long)check);
}
