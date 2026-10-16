"""Genera data/ch1/playthrough_expected.json: golden path del cap 1 (01 v3.3).
Riusa la logica di econ_sim_v3.py (stesse formule e stessa politica 'miglior valore per LN')
e aggiunge l'onboarding di 04_UI §4. Senza offline (in editor si gioca in continuo).
Uso: python3 playthrough_ch1.py [--out path]
"""
import sys, json, argparse, importlib.util, os
ap=argparse.ArgumentParser(); ap.add_argument("--out",default=os.path.join(os.path.dirname(__file__),"..","data","ch1","playthrough_expected.json")); OA=ap.parse_args()
HERE=os.path.dirname(os.path.abspath(__file__))
def load(eff):
    sys.argv=["econ_sim_v3.py","--eff",str(eff),"--sess","none"]
    spec=importlib.util.spec_from_file_location(f"sim{eff}",os.path.join(HERE,"econ_sim_v3.py")); m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m

# Onboarding 04_UI §4 (secondi): reddito solo dal primo cliente
ONB=[("moveTutorialStart",15,5,"Card 'Trascina per muovere Nina'"),
     ("firstParcelPicked",30,10,"Prima pila sulla Zona Arrivi (MAX a 5)"),
     ("firstShelfDeposit",50,10,"Scaffale Terracotta"),
     ("firstCustomerServed",70,15,"Mamma Rosa al Bancone"),
     ("firstCoinsCollected",75,15,"Monete a terra davanti al Bancone"),
     ("firstCombo",100,15,"Due clienti ravvicinati x1,1 -> x1,2"),
     ("nonnoHired",130,5,"Pad 'ASSUMI NONNO · 50 LN'"),
     ("phoneRings",165,10,"Messaggi (Civetta, teaser)"),
     ("ordersApp",210,10,"Ordine speciale Mamma Rosa"),
     ("firstCassaCollection",235,10,"Nina passa sulla Cassa: mancia una tantum di 30 LN (nessun offline)"),
     ("firstDebtDeposit",240,10,"Cassaforte: card 'Giorno 3/365 · Rata 1 · 8.400 LN · scade Giorno 24'"),
     ("wallPadShown",270,10,"Pad APRI MURO come obiettivo successivo")]
_D=os.path.join(HERE,'..','data','ch1'); _UP=json.load(open(os.path.join(_D,'upgrades.json'))); _RW=json.load(open(os.path.join(_D,'economy.json'))).get('rewards',{})
ZCOST={L['level']:L['costLn'] for L in _UP['zaino']['levels']}
MIL=[(x['parcels'],x['bonusLn']) for x in _RW.get('parcelMilestones',[])]; CASSA_TIP=_RW.get('cassaFirstVisitTipLn',0)
REP50=any(x['reward']=='reputation' for x in _RW.get('debtMilestones',[]))
PADS_REVEAL=170   # passo 'padNuovi' di onboarding.json
FIRST_INCOME=70; DEPOSIT_EVERY=60; DEPOSIT_START=240

def play(eff, log=True):
    m=load(eff); lv={k:0 for k in m.LINES}; c=1; dt=1.0
    t=0.0; earned=0.0; wallet=0.0; debt_share=0.0; deposited=0.0; nonno=False; k=0.0; srv=0.0
    parcels=[0.0]; got=set(); mils=[]; ev=[]; cps=[]; buys=[]; deposits=[]; trace={}
    names={'zaino':'ZAINO','scarpe':'SCARPE','insegna':'INSEGNA','nonno':'NONNO LV','pippa':'PIPPA','braccio':'BRACCIO'}
    while deposited < m.TRANCHE[0] and t < 3600:
        hired = nonno
        if t>=FIRST_INCOME:
            st=m.ZAINO[lv['zaino']]; p,gap=m.nina_rate(c,lv,hired); n=min(p,m.CUST(c))
            srv+=n*dt/60
            if srv>=st: srv-=st; k = k if gap<=m.COMBO_WIN else 0
            cb=min(m.COMBO_MAX,1+m.COMBO_STEP*k); k+=n*dt/60
            inc,_=m.income_rate(c,lv,hired,False,cb); inc*=dt/60*(1+0.05*0.1*(REP50 and deposited>=0.5*m.TRANCHE[0])); parcels[0]+=n*dt/60  # +1 Reputazione = +5% sui VIP (10% dei clienti)
            earned+=inc
            sp = 1.0 if t<DEPOSIT_START else m.SPEND   # prima della card debito (4:00) tutte le LN restano nel portafoglio
            wallet+=inc*sp; debt_share+=inc*(1-sp)
        if not nonno and t>=m.NONNO_T*60 and wallet>=m.NONNO_COST:
            wallet-=m.NONNO_COST; nonno=True; buys.append(dict(t=t,pad="hireNonno",label="ASSUMI NONNO · 50 LN",costLn=50,level=1))
        # onboarding graduale (16/10): Zaino 1 dal passo 6; altri livelli Zaino da NONNO; Scarpe/Insegna/NONNO LV dal passo 'pad nuovi' (PADS_REVEAL)
        if t>=PADS_REVEAL: vis=m.LINES
        elif nonno: vis={'zaino':m.LINES['zaino']}
        else: vis={'zaino':m.LINES['zaino']} if lv['zaino']==0 else {}
        while True:
            base,_=m.income_rate(c,lv,nonno,False,m.est_combo(c,lv,nonno)); best=None
            for key,(b,g,mx,cu) in vis.items():
                if lv[key]>=mx or c<cu: continue
                cost=(ZCOST[lv[key]+1] if key=='zaino' and lv[key]+1 in ZCOST else b*g**lv[key]); lv[key]+=1
                gain=m.income_rate(c,lv,nonno,False,m.est_combo(c,lv,nonno))[0]-base; lv[key]-=1
                if key=='zaino' and lv[key]==0 and t<m.NONNO_T*60: gain=max(gain,1e-9)
                if gain>0 and (best is None or gain/cost>best[0]): best=(gain/cost,cost,key)
            if best and wallet>=best[1]:
                wallet-=best[1]; lv[best[2]]+=1
                buys.append(dict(t=t,pad=("nonnoUpgrade" if best[2]=='nonno' else best[2]),label=f"{names[best[2]]} {lv[best[2]]} · {round(best[1])} LN",costLn=round(best[1]),level=lv[best[2]]))
            else: break
        for th,bon in MIL:
            if parcels[0]>=th and th not in got: got.add(th); wallet+=bon; earned+=bon; mils.append(dict(t=t,parcels=th,bonusLn=bon))
        if CASSA_TIP and t==235: wallet+=CASSA_TIP; earned+=CASSA_TIP   # mancia di Nonna Ada al primo passaggio sulla Cassa
        if t==DEPOSIT_START: x=0.6*wallet; wallet-=x; debt_share+=x   # primo versamento: 60% del portafoglio
        if t>=DEPOSIT_START and (t-DEPOSIT_START)%DEPOSIT_EVERY==0 and debt_share-deposited>0:
            x=min(debt_share-deposited, m.TRANCHE[0]-deposited); deposited+=x; deposits.append(dict(t=t,amountLn=round(x),totalDepositedLn=round(deposited)))
        trace[int(t)]=round(earned)
        if t%60==0: cps.append(dict(t=t,cumulativeEarnedLn=round(earned),walletLn=round(wallet),depositedLn=round(deposited),zaino=lv['zaino'],scarpe=lv['scarpe'],insegna=lv['insegna'],nonnoLevel=lv['nonno']))
        if deposited>=m.TRANCHE[0]: break
        t+=dt
    return dict(mils=mils,trace=trace,endSec=t,buys=buys,deposits=deposits,cps=cps,earned=earned,lv=dict(lv))

ref=play(1.0); lo=play(1.2); hi=play(0.85)
def at(ts): return ref['trace'].get(int(ts),0)
ms=[dict(id=i,tSec=s,toleranceSec=tol,note=n,cumulativeEarnedLnApprox=at(s)) for i,s,tol,n in ONB]
z1=next(b for b in ref['buys'] if b['pad']=='zaino')
ms.append(dict(id="zainoLv1Purchased",tSec=z1['t'],toleranceSec=20,note="Pad 'ZAINO LV 1 · 40 LN' nel Magazzino",cumulativeEarnedLnApprox=at(z1['t'])))
ms.append(dict(id="tranchePaidChapterEnd",tSec=ref['endSec'],toleranceSec=round(max(ref['endSec']-lo['endSec'],hi['endSec']-ref['endSec'])),
  minSecEff12=lo['endSec'],maxSecEff085=hi['endSec'],note="Rata 1 saldata (8.400 LN), fine capitolo 1",cumulativeEarnedLnApprox=round(ref['earned'])))
ms.sort(key=lambda x:x['tSec'])
buys=[dict(**b, toleranceSec=max(15,round(0.15*b['t']))) for b in ref['buys']]
# azioni del bot
steps=[
 dict(tSec=0,action="wait",holdSec=15,note="cold open, HUD nascosto"),
 dict(tSec=15,action="joystickDrag",distanceM=1.5),
 dict(tSec=30,action="moveTo",zoneId="arrivi",holdOnZoneSec=1.0,expect="stack=5, MAX visibile"),
 dict(tSec=50,action="moveTo",zoneId="shelfTerracotta",holdOnZoneSec=1.0,expect="5 pacchi sullo scaffale"),
 dict(tSec=60,action="moveTo",zoneId="shelfTerracotta",holdOnZoneSec=1.0,expect="pila presa dallo scaffale"),
 dict(tSec=70,action="moveTo",zoneId="bancone",holdOnZoneSec=5.5,expect="clienti serviti, monete a terra"),
 dict(tSec=75,action="moveTo",zoneId="coinDrop",holdOnZoneSec=1.5,expect="LN nella pillola valute"),
 dict(tSec=80,action="loop",loopId="preNonno",untilSec=130),
 dict(tSec=130,action="moveTo",zoneId="padHireNonno",holdOnZoneSec=3.0,expect="NONNO attivo, restock 6 pacchi/min"),
 dict(tSec=165,action="tapPhone",then="quickReply:Chi sei?"),
 dict(tSec=210,action="openApp",app="Ordini",then="Mostra la strada"),
 dict(tSec=235,action="moveTo",zoneId="cassa",holdOnZoneSec=1.0,expect="+30 LN mancia di Nonna Ada (solo la prima volta)"),
 dict(tSec=240,action="moveTo",zoneId="cassaforte",holdOnZoneSec=3.0,expect="card debito Giorno 3/365, primo versamento"),
 dict(tSec=250,action="loop",loopId="postNonno",untilEvent="tranchePaidChapterEnd")]
for b in buys:
    if b['pad']!='hireNonno': steps.append(dict(tSec=b['t'],action="moveTo",zoneId={"zaino":"padZaino","scarpe":"padScarpe","insegna":"padInsegna","nonnoUpgrade":"padNonnoUpgrade"}[b['pad']],holdOnZoneSec=3.0,expect=b['label']))
for d in ref['deposits'][1:]:
    steps.append(dict(tSec=d['t'],action="moveTo",zoneId="cassaforte",holdOnZoneSec=3.0,expect=f"versati {d['totalDepositedLn']} LN"))
steps.sort(key=lambda s:s['tSec'])
for b in buys: b['cumulativeEarnedLnApprox']=at(b['t'])
out=dict(source="01_CORE_LOOP v3.3 + data/ch1 + 04_UI §4; generato da qa/playthrough_ch1.py (logica qa/econ_sim_v3.py)",
 chapter=1,referenceEff=1.0,offline=False,
 assumptions=dict(incomeStartsAtSec=FIRST_INCOME,debtDepositEverySec=DEPOSIT_EVERY,firstDebtDepositSec=DEPOSIT_START,
   playerSpendShare=0.4,playerSpendShareBeforeFirstDebtDeposit=1.0,firstDebtDepositShareOfWallet=0.6,purchasePolicy="bestLnPerMinGainPerLnSpent, gain>0",
   note="La sim di riferimento (econ_sim_v3.py, eff 1.0, nessun offline) da' 34,1 min per il cap 1 perche' incassa da t=0; qui il reddito parte al primo cliente (70 s) e il versamento e' ogni 60 s. ~30,5 min e' il profilo con offline 4x5."),
 loops=dict(
   preNonno=[dict(zoneId="arrivi",holdOnZoneSec=1.0),dict(zoneId="shelfTerracotta",holdOnZoneSec=1.0),dict(zoneId="shelfTerracotta",holdOnZoneSec=1.0),dict(zoneId="bancone",holdOnZoneSec=5.5),dict(zoneId="coinDrop",holdOnZoneSec=1.5)],
   postNonno=[dict(zoneId="shelfTerracotta",holdOnZoneSec=1.0),dict(zoneId="bancone",holdOnZoneSec="stack*1.0+0.5"),dict(zoneId="coinDrop",holdOnZoneSec=1.5),dict(zoneId="arrivi",holdOnZoneSec=1.0,when="shelfStock<stack (Nina rifornisce la quota non coperta da NONNO)")]),
 milestones=ms, parcelMilestones=ref['mils'], purchases=buys, debtDeposits=ref['deposits'], checkpoints=ref['cps'],
 chapterEnd=dict(eff10Sec=ref['endSec'],eff12Sec=lo['endSec'],eff085Sec=hi['endSec'],windowSec=2400,trancheLn=8400,dueCalendarDay=24),
 finalLevels=ref['lv'], botSteps=steps)
json.dump(out,open(OA.out,'w'),indent=2,ensure_ascii=False)
print("end eff1.0",ref['endSec'],"eff1.2",lo['endSec'],"eff0.85",hi['endSec'])
for b in ref['buys']: print(b['t'],b['label'])
for x in ms: print(x['id'],x['tSec'],x.get('cumulativeEarnedLnApprox'))
