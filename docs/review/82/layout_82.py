# Modello 2D (XZ) delle zone a terra del Cap.1: vecchio (main 7011805) vs nuovo (patch_187_layout.diff). Raggio visivo = Radius*1.075 (POI_Rim = Radius*2.15 di diametro).
import sys
BZ=float(sys.argv[1]);CASSA=(float(sys.argv[2]),float(sys.argv[3]))
import math, matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
from matplotlib.patches import Circle, Rectangle
arr=(-5,6); shelfC=(-5,0); ban=(2.52,-5.64)
def ad(a,b): return (a[0]+b[0],a[1]+b[1])
def rad_old(sx,sz): return min(max(min(sx,sz)/2,0.45),1.2)
def rad_new(sx,sz): return min(max(min(sx,sz)/2-0.1,0.4),0.8)
def zones(new):
    Z=[]; R=rad_new if new else rad_old
    Z.append(("Zona_Arrivi","Prendi",arr,(1.8,1.8) if new else (2.6,2.6)))
    for i,c in enumerate("ABC"):
        p=(arr[0]+(i-1)*2.2,0)
        Z.append((f"Zona_Scaffale_{c}","Consegna",ad(p,(0,0.65 if new else 0.6)),(1.8,1.2)))
        Z.append((f"Zona_Prendi_{c}","Prendi",ad(p,(0,2.0 if new else 1.6)),(1.8,0.7)))
    Z.append(("Zona_Bancone","Consegna",ad(ban,(0,BZ)) if new else ban,(2.4,1.2)))
    Z.append(("Zona_Cassa","Prendi",ad(ban,CASSA) if new else ad(ban,(3.2,1.5)),(1.2,1.2) if new else (1.4,1.4)))
    pr=ad(ban,(-0.9,4.5)) if new else ad(ban,(-1.5,4.5))
    for k,n in enumerate(["Pad_Zaino","Pad_Scarpe","Pad_Insegna"]): Z.append((n,"Acquisto",ad(pr,((1.85 if new else 1.8)*k,0)),(1.4,1.4)))
    Z.append(("Pad_AssumiNonno","Acquisto",ad(arr,(3.2,-0.3 if new else 0.5)),(1.4,1.4)))
    out=[]
    for n,k,p,s in Z:
        if new:  # #187 rev.2 (Game Designer): anello = trigger, diametro esterno = lato minore del trigger
            rv=min(s)/2; r=rv/1.075
        else:
            r=0.95 if n.startswith("Pad_") else R(*s); rv=r*1.075
        out.append(dict(name=n,kind=k,pos=p,trig=s,r=r,rv=rv))
    return out
def props(new):  # ostacoli (cerchio r o rettangolo cx,cz,w,d) da DepotKit/SliceSceneBuilder
    P=[("vaso limoni (deposito)",("c",(-3.1+1.4,7.4),0.35)),("pila pacchi N",("c",(-3.0,7.5),0.5)),("planter",("r",(0,7.7),1.2,0.5)),
       ("vaso limoni corsia",("c",(-8.6,-0.6),0.35)),("vaso tondo corsia",("c",(-1.4,-0.6),0.3)),("pacco cubo",("c",(-6.1,-1.4),0.3)),("pacco cubo",("c",(-3.8,-1.4),0.3)),
       ("Bancone (banco)",("r",ad(ban,(0,-0.9)),2.4,0.7)),("vaso limoni Bancone",("c",ad(ban,(-1.75,-0.5)),0.35)),("pila Bancone",("c",ad(ban,(-1.8,0.6)),0.5)),
       ("lavagna",("c",ad(ban,(-1.2,-2.4)),0.35)),("lampione",("c",ad(ban,(-1.9,-3.4)),0.15)),
       ("vaso tondo Bancone",("c",ad(ban,(-2.6,1.4) if new else (1.6,0.7)),0.3)),
       ("muro Bancone",("r",ad(ban,(0.8,1.45)),4.4,0.48)),("arco Bancone",("r",ad(ban,(3.2,-0.3)),0.48,4.0)),
       ("Nonna Ada",("c",(4.12,-5.79),0.3)),("panca E",("r",(6.9,-4),0.5,1.5)),("vaso E",("c",(6.9,-3.3),0.35)),
       ("Scaffale A",("r",(-7.2,-0.6),1.8,0.5)),("Scaffale B",("r",(-5,-0.6),1.8,0.5)),("Scaffale C",("r",(-2.8,-0.6),1.8,0.5)),
       ("spawn Nina",("c",(0,-1),0.3)),("spawn NONNO",("c",(-2.5,4),0.45)),("staccionata O",("r",(-9.15,0),0.42,22)),("muro N",("r",(0,8.35),18,0.48)),("muro E",("r",(7.6,0),0.48,20))]
    return P
def gap_cc(a,ra,b,rb): return math.dist(a,b)-ra-rb
def gap_cr(c,r,rc,w,d):
    dx=max(abs(c[0]-rc[0])-w/2,0); dz=max(abs(c[1]-rc[1])-d/2,0); return math.hypot(dx,dz)-r
def check(new,minGap=0.4):
    Z=zones(new); P=props(new); issues=[]
    for i in range(len(Z)):
        for j in range(i+1,len(Z)):
            g=gap_cc(Z[i]["pos"],Z[i]["rv"],Z[j]["pos"],Z[j]["rv"])
            if g<minGap: issues.append((Z[i]["name"],Z[j]["name"],round(g,2)))
        for n,sh in P:
            g=gap_cc(Z[i]["pos"],Z[i]["rv"],sh[1],sh[2]) if sh[0]=="c" else gap_cr(Z[i]["pos"],Z[i]["rv"],sh[1],sh[2],sh[3])
            if g<minGap: issues.append((Z[i]["name"],n,round(g,2)))
    return Z,issues
COL={"Prendi":"#C8633B","Consegna":"#8FC9A8","Acquisto":"#F4C430"}
fig,axs=plt.subplots(1,2,figsize=(16,10))
for ax,new in zip(axs,(False,True)):
    Z,iss=check(new)
    for n,sh in props(new):
        if sh[0]=="c": ax.add_patch(Circle(sh[1],sh[2],color="#7A4A2E",alpha=0.5))
        else: ax.add_patch(Rectangle((sh[1][0]-sh[2]/2,sh[1][1]-sh[3]/2),sh[2],sh[3],color="#CDAE82",alpha=0.8))
    for z in Z:
        ax.add_patch(Rectangle((z["pos"][0]-z["trig"][0]/2,z["pos"][1]-z["trig"][1]/2),*z["trig"],fill=False,ls="--",ec="#3B2A20",lw=0.8))
        ax.add_patch(Circle(z["pos"],z["rv"],color=COL[z["kind"]],alpha=0.75)); ax.text(z["pos"][0],z["pos"][1],z["name"].replace("Zona_","").replace("Pad_",""),fontsize=7,ha="center",va="center")
    bad={a for a,b,g in iss}|{b for a,b,g in iss}
    for z in Z:
        if z["name"] in bad: ax.add_patch(Circle(z["pos"],z["rv"],fill=False,ec="red",lw=2))
    ax.set_xlim(-9.6,8);ax.set_ylim(-11.5,9);ax.set_aspect("equal");ax.grid(alpha=0.2)
    ax.set_title(("NUOVO (#187 rev.2, anello = trigger)" if new else "VECCHIO (main 7011805)")+f" — conflitti <0,4 m: {len(iss)}"); ax.set_xlabel("X (m)"); ax.set_ylabel("Z (m)")
fig.suptitle("Zone a terra Cap.1, vista dall'alto. Cerchio = disco visivo (Radius×1,075), tratteggio = trigger, rosso = gap <0,4 m",fontsize=11)
plt.tight_layout(); plt.savefig("layout_82.png",dpi=110)
for new in (False,True):
    Z,iss=check(new); print("NEW" if new else "OLD")
    for z in Z: print(f'  {z["name"]};{z["kind"]};{z["pos"][0]:.2f};{z["pos"][1]:.2f};{z["trig"][0]}x{z["trig"][1]};{z["r"]:.2f};{z["rv"]:.2f}')
    for i in iss: print("  ISSUE",i)
