"""Simulazione economia v3.3 - Ultimo Miglio (Game Designer, 01_CORE_LOOP v3.3).
Sim di design. Sim QA indipendente: econ_sim.py (storico: econ_sim_v1_qa.py, econ_sim_v3_qa.py).
Uso: python3 econ_sim_v3.py --eff 1.0 --sess 4x5 [--ad] [--assist] [--tranche '[...]']
  --sess NxL  = N sessioni al giorno da L minuti attivi; "none" = offline mai ritirato
  --ad        = x2 rewarded alla Cassa (max 2 volte al giorno)
  --assist    = Modalita' Assist (08_RETENTION §8): tranche -15%, 1 ciclo = 4 giorni, finestra combo 7 s
eff scala SOLO il lavoro manuale di Nina (camminata e reazione sulle zone).
"""
import argparse, json
ap=argparse.ArgumentParser(); ap.add_argument("--eff",type=float,default=1.0)
ap.add_argument("--sess",default="4x5"); ap.add_argument("--ad",action="store_true")
ap.add_argument("--assist",action="store_true"); ap.add_argument("--boost",action="store_true",help="totem rewarded Caffe' Turbo + Zaino Gonfio a inizio di ogni sessione (max 4+4/giorno)"); ap.add_argument("--tdn",action="store_true",help="Turno di notte (pad cap 3): Cassa 6%% invece di 4%%"); ap.add_argument("--tranche",default=None)
A=ap.parse_args(); EFF=A.eff

# ---------------- DESIGN 01 v3.3 ----------------
V=lambda c:10*1.5**(c-1)                      # LN/pacco
CUST=lambda c:30+5*(c-1)                      # clienti/min in arrivo (> capacita' di Nina)
PATIENCE=20                                   # s di pazienza in coda (dimensiona la coda)
TRANCHE=[8400,18000,33000,70000,117000,130000,205000,325000,505000,790000,1200000,1950000,3000000,4500000,6800000]
if A.tranche: TRANCHE=json.loads(A.tranche)
PAD=[0,400,800,1500,2500,4000,6000,9000,13000,19000,27000,38000,55000,80000,115000]
NONNO_COST, NONNO_T = 50, 13/6
ZAINO=[5,6,7,8,9,10,12,14,16,18,20,22,24,27,30]
LINES={ # nome: (costo base, crescita, max liv, cap sblocco)
 'zaino':(40,2.0,14,1),'scarpe':(60,2.7,10,1),'insegna':(90,2.7,10,1),
 'nonno':(100,2.4,10,1),'pippa':(600,2.4,10,3),'braccio':(15000,2.4,10,8)}
ZAINO_OVR={5:560,6:960,7:1700,8:3000}   # 16/10 GD: Zaino 5-7 a x1,7, Zaino 8 a 3.000 (data/ch1/upgrades.json)
INSEGNA_VAL=0.06
SPEED,PICK,SERVE,COIN,DWELL,SLOW=3.0,0.1,1.0,1.5,0.5,0.10
D_BANCO=lambda c:(9+0.4*c)*(0.75 if c>=4 else 1)
D_ARRIVI=6.0
NONNO_PPM=lambda lv:6+1.5*lv
def auto_ppm(c,lv):
    a=0.5+0.2*lv['nonno']
    if c>=3: a+=1+0.5*lv['pippa']
    if c>=4: a+=2
    if c>=8: a+=1+0.5*lv['braccio']
    return a
NIGHT_FRAC=lambda c:2/7 if c>=6 else 0
COMBO_STEP,COMBO_MAX=0.1,2.0
COMBO_WIN=7.0 if A.assist else 5.0
SPEND=0.4
# offline (solo Cassa)
OFF_EFF=0.5                                   # 50% del throughput reale dei robot
CASSA_FRAC=0.04                               # capienza Cassa = 4% della tranche
OFF_SHARE_MAX=0.12 if A.assist else 0.20                            # offline <= 20% delle LN del capitolo (= 1/4 delle LN attive)
AD_PER_DAY=2
TDN_FRAC=0.06                                 # con Turno di notte (pad cap 3, 2.500 LN)
BOOST_MIN, CAFFE, ZAINO_G, BOOST_PER_DAY = 2, 0.30, 0.50, 4   # 03 §2: Scarpe +30% / pila +50% per 2 min; 01 v3.3: 1 totem per sessione, max 4/giorno
# scadenza
CYCLE_MIN=7; DAYS_PER_CYCLE=4.0 if A.assist else 365/15*7/40   # finestra 40 min (normale) / 42,6 (Assist)
DAY_MIN=CYCLE_MIN/DAYS_PER_CYCLE              # minuti attivi per giorno di calendario
DUE=lambda k: 365*k/15*DAY_MIN                # scadenza assoluta rata k (min attivi)
MIN_WINDOW=365/15*DAY_MIN                     # ogni rata ha almeno 24,3 giorni dal pagamento precedente (40 min; 42,6 in Assist)
PROROGA_MIN=CYCLE_MIN; TOKENS_MAX=3; ACT=lambda c:1 if c<=3 else 2 if c<=8 else 3 if c<=12 else 4
MORA_INT, PEDAGGIO = 0.10, 0.15               # mora: +10% tranche, pedaggio -15% su ogni LN finche' non si paga
if A.assist: TRANCHE=[x if i<3 else round(x*0.90) for i,x in enumerate(TRANCHE)]   # Assist: -10% dal cap 4 (cap 1-3 gia' -5%)
# -------------------------------------------------
if A.sess=="none": NS,SL=0,0
else: NS,SL=map(int,A.sess.split("x"))

BOOST=[False]
def nina_rate(c,lv,hired):
    st=ZAINO[lv['zaino']]; v=SPEED*(1+0.06*lv['scarpe'])*EFF
    dw=DWELL/EFF
    if BOOST[0]=='z': st=round(st*(1+ZAINO_G))
    if BOOST[0]=='c': v*=1+CAFFE
    walk=st*PICK+2*D_BANCO(c)/(v*(1-SLOW/2))+2*dw     # giro senza il servizio
    serve=(walk+st*SERVE+COIN)/st
    restock=(2*st*PICK+2*D_ARRIVI/v+2*dw)/st
    if c>=4: p=60/serve; gap=walk+COIN
    else:
        p=60/(serve+restock)
        if hired:
            for _ in range(30): p=60/(serve+(1-min(1,NONNO_PPM(lv['nonno'])/p))*restock)
        cov=1 if not hired else min(1,NONNO_PPM(lv['nonno'])/p)
        gap=walk+COIN+(1-cov)*restock*st           # tempo tra l'ultimo cliente di un giro e il primo del successivo
    return p,gap
def income_rate(c,lv,hired,mora,combo_avg):
    dem=CUST(c); p,_=nina_rate(c,lv,hired); n=min(p,dem)
    auto=min(auto_ppm(c,lv),max(0,dem-n))
    mult=V(c)*(1+INSEGNA_VAL*lv['insegna'])*(1-NIGHT_FRAC(c))*(1-PEDAGGIO if mora else 1)
    return (n*combo_avg+auto)*mult, n
def est_combo(c,lv,hired):                     # combo media a regime (per scegliere gli upgrade)
    st=ZAINO[lv['zaino']]*(1+ZAINO_G if BOOST[0]=='z' else 1); _,gap=nina_rate(c,lv,hired); k=0
    st=round(st); tot=0; N=0
    for _ in range(6):
        if gap>COMBO_WIN: k=0
        for _ in range(st): tot+=min(COMBO_MAX,1+COMBO_STEP*k); k+=1; N+=1
    return tot/N
def run():
    lv={k:0 for k in LINES}; bank=0.0; rows=[]; clock=0.0; tokens=0; late=0
    day_min=0.0; ads_today=0; sess_left=SL; boosts_today=0
    for c in range(1,16):
        if c==1 or ACT(c)!=ACT(c-1): tokens=min(TOKENS_MAX,tokens+1)
        due=DUE(c)
        due=max(due,clock+MIN_WINDOW)              # sempre almeno una finestra piena dal pagamento precedente
        mora=False; used=False; need=TRANCHE[c-1]; pad=PAD[c-1]; paid=0.0; t=0.0; dt=1/60
        nonno=c>1; act=off=0.0; k=0; served_in_trip=0.0; combo_sum=0.0; combo_n=0
        while (paid<need or pad>0) and t<150:
            if not mora and clock+t>due:
                if tokens>0 and not used: tokens-=1; used=True; due+=PROROGA_MIN
                else: mora=True; need+=MORA_INT*TRANCHE[c-1]
            hired=nonno and (c>1 or t>=NONNO_T)
            BOOST[0]=('z' if boosts_today%2 else 'c') if (A.boost and c>=4 and NS>0 and (SL-sess_left)<BOOST_MIN and boosts_today<BOOST_PER_DAY) else False
            st=round(ZAINO[lv['zaino']]*(1+ZAINO_G if BOOST[0]=='z' else 1)); p,gap=nina_rate(c,lv,hired); n=min(p,CUST(c))
            # combo vera, giro per giro: a inizio giro si azzera se il buco supera la finestra
            served_in_trip+=n*dt
            if served_in_trip>=st: served_in_trip-=st; k = k if gap<=COMBO_WIN else 0
            cb=min(COMBO_MAX,1+COMBO_STEP*k); k_inc=n*dt; k+=k_inc
            combo_sum+=cb*n*dt; combo_n+=n*dt
            inc,_=income_rate(c,lv,hired,mora,cb); inc*=dt; act+=inc
            # sessioni e Cassa
            if NS:
                sess_left-=dt; day_min+=dt
                if sess_left<=0:
                    sess_left=SL
                    gap_h=24/NS-SL/60
                    if day_min>=NS*SL-1e-9: day_min=0; ads_today=0; boosts_today=0
                    if A.boost: boosts_today+=1
                    if hired or c>1:
                        o=min(OFF_EFF*auto_ppm(c,lv)*V(c)*(1+INSEGNA_VAL*lv['insegna'])*60*gap_h, (TDN_FRAC if (A.tdn and c>=3) else CASSA_FRAC)*TRANCHE[c-1])
                        if A.ad and ads_today<AD_PER_DAY: o*=2; ads_today+=1
                        o=max(0,min(o,act*OFF_SHARE_MAX/(1-OFF_SHARE_MAX)-off))   # tetto offline
                        if mora: o*=1-PEDAGGIO
                        inc+=o; off+=o
            bank+=inc*SPEND; paid+=inc*(1-SPEND)
            if paid>need: bank+=paid-need; paid=need
            if not nonno and t>=NONNO_T and bank>=NONNO_COST: bank-=NONNO_COST; nonno=True
            if pad>0: x=min(bank,pad); bank-=x; pad-=x
            BOOST[0]=False
            # upgrade: miglior guadagno di LN/min per LN spesa
            while True:
                base,_=income_rate(c,lv,hired,False,est_combo(c,lv,hired)); best=None
                for key,(b,g,mx,cu) in LINES.items():
                    if lv[key]>=mx or c<cu: continue
                    cost=(ZAINO_OVR[lv[key]+1] if key=='zaino' and lv[key]+1 in ZAINO_OVR else b*g**lv[key]); lv[key]+=1
                    gain=income_rate(c,lv,hired,False,est_combo(c,lv,hired))[0]-base; lv[key]-=1
                    if gain>0 and (best is None or gain/cost>best[0]): best=(gain/cost,cost,key)
                if best and bank>=best[1]: bank-=best[1]; lv[best[2]]+=1
                else: break
            t+=dt
        clock+=t; late+=mora
        rows.append(dict(cap=c,min=round(t,1),z=lv['zaino'],s=lv['scarpe'],i=lv['insegna'],nonno=lv['nonno'],pippa=lv['pippa'],
            combo=round(combo_sum/max(combo_n,1e-9),2),off=round(100*off/(act+off)),clock=round(clock),due=round(due),
            stato="MORA" if mora else ("proroga" if used else "ok")))
    return rows,clock,late,tokens
if __name__=="__main__":
    rows,clock,late,tok=run()
    print(f"eff={EFF} sess={A.sess} ad={A.ad} assist={A.assist} boost={A.boost} tdn={A.tdn}")
    print("cap|tranche|min attivi|Zaino|Scarpe|Insegna|NONNO|PIPPA|combo media|%offline|min cumul.|scadenza|stato")
    for r in rows: print("|".join(str(x) for x in [r['cap'],TRANCHE[r['cap']-1],r['min'],r['z'],r['s'],r['i'],r['nonno'],r['pippa'],r['combo'],r['off'],r['clock'],r['due'],r['stato']]))
    print(f"Totale {round(clock)} min ({clock/60:.1f} h); range {min(r['min'] for r in rows)}-{max(r['min'] for r in rows)} min; more {late}; proroghe usate {sum(r['stato']=='proroga' for r in rows)}")
