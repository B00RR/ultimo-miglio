"""Valuta le proposte LiveOps (liveops/REVISIONE_ECONOMIA_CAP1.md) sul golden path del cap 1.
Riusa play() di playthrough_ch1.py con agganci: costi Zaino, traguardi pacchi, tappe debito, mancia Cassa, freccia pad.
Uso: python3 qa/eval_liveops_ch1.py"""
import sys, os, re
HERE=os.path.dirname(os.path.abspath(__file__))
src=open(os.path.join(HERE,"playthrough_ch1.py")).read().split("ref=play(1.0)")[0]
src=src.replace("ap.parse_args()","ap.parse_args([])").replace("os.path.dirname(os.path.abspath(__file__))",repr(HERE))
# agganci
src=src.replace("cost=b*g**lv[key]","cost=COST(key,b,g,lv[key])")
src=src.replace("inc,_=m.income_rate(c,lv,hired,False,cb); inc*=dt/60",
 "inc,_=m.income_rate(c,lv,hired,False,cb); inc*=dt/60*(1+REP*0.05*0.1*(deposited>=0.5*m.TRANCHE[0])); parcels[0]+=n*dt/60")
src=src.replace("        if t==DEPOSIT_START:",
"""        for th,bon in MIL:
            if parcels[0]>=th and th not in got: got.add(th); wallet+=bon; earned+=bon; mils.append((t,th))
        if CASSA and t==235: wallet+=CASSA; earned+=CASSA
        if t==DEPOSIT_START:""")
src=src.replace("ev=[]; cps=[]","parcels=[0.0]; got=set(); mils=[]; ev=[]; cps=[]")
src=src.replace("return dict(trace","return dict(mils=mils,trace")
ZT={}; MIL=[]; REP=0; CASSA=0; ARROW=False
def COST(key,b,g,l):
    if key=='zaino' and (l+1) in ZT: return ZT[l+1]
    return b*g**l
exec(src)
def run(name,zt={},mil=[],rep=0,cassa=0):
    global ZT,MIL,REP,CASSA
    ZT,MIL,REP,CASSA=zt,mil,rep,cassa
    r={e:play(e) for e in (0.7,0.85,1.0,1.2)}
    b=r[1.0]['buys']; late=[x for x in b if x['t']>=480]
    gaps=[round((late[i+1]['t']-late[i]['t'])/60,1) for i in range(len(late)-1)]
    print(f"{name:34s} fine: "+" ".join(f"e{e}={r[e]['endSec']/60:.1f}" for e in r)+
      f" | acquisti dopo 8:00 eff1: "+", ".join(f"{x['label']}@{int(x['t']//60)}:{int(x['t']%60):02d}" for x in late)+
      f" | traguardi: {[(th,f'{int(t//60)}:{int(t%60):02d}') for t,th in r[1.0]['mils']]}")
    return r
Z={5:560,6:960,7:1700}; M=[(100,80),(250,150),(450,250)]
run("base")
run("1 Zaino 560/960/1700",zt=Z)
run("2 traguardi pacchi",mil=M)
run("3 tappa 50% +1 Rep",rep=1)
run("4 mancia Cassa 30",cassa=30)
run("1+2+3+4",zt=Z,mil=M,rep=1,cassa=30)
run("1+3+4 (senza 2)",zt=Z,rep=1,cassa=30)
run("1+2r+3+4 (traguardi 100/250/450 = 50/100/150)",zt=Z,mil=[(100,50),(250,100),(450,150)],rep=1,cassa=30)
