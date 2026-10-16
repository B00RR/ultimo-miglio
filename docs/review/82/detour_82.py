# #97/#82: percorso più breve di Nina (r 0,3) pad -> Zona_Bancone, con varco ovest chiuso (vaso -0,5) e aperto (vaso alla posizione vecchia -0,9).
# Metodo: griglia 5 cm, ostacoli di layout_82.py gonfiati di 0,3 m, Dijkstra 8-connesso (lunghezze euclidee).
import sys, math, heapq, importlib.util, io, contextlib
sys.argv=["x","0.33","2.8","2.8"]
spec=importlib.util.spec_from_file_location("L","layout_82.py"); L=importlib.util.module_from_spec(spec)
import matplotlib; matplotlib.use("Agg")
with contextlib.redirect_stdout(io.StringIO()): spec.loader.exec_module(L)
ban=L.ban; R=0.3; H=0.05; X0,X1,Z0,Z1=-9.5,8,-11.5,9
def blocked(props):
    nx=int((X1-X0)/H)+1; nz=int((Z1-Z0)/H)+1; B=bytearray(nx*nz)
    for _,sh in props:
        for i in range(nx):
            x=X0+i*H
            for j in range(nz):
                z=Z0+j*H
                if sh[0]=="c": g=math.dist((x,z),sh[1])-sh[2]
                else: g=L.gap_cr((x,z),0,sh[1],sh[2],sh[3])
                if g<R: B[i*nz+j]=1
    return B,nx,nz
def path(B,nx,nz,a,b):
    idx=lambda p:(round((p[0]-X0)/H),round((p[1]-Z0)/H))
    s=idx(a); t=idx(b); D={s:0}; q=[(0,s)]
    while q:
        d,u=heapq.heappop(q)
        if u==t: return d
        if d>D[u]: continue
        for di in(-1,0,1):
            for dj in(-1,0,1):
                if not di and not dj: continue
                v=(u[0]+di,u[1]+dj)
                if not(0<=v[0]<nx and 0<=v[1]<nz) or B[v[0]*nz+v[1]]: continue
                nd=d+H*math.hypot(di,dj)
                if nd<D.get(v,1e9): D[v]=nd; heapq.heappush(q,(nd,v))
    return float("inf")
P=[p for p in L.props(True) if not p[0].startswith("spawn")]
CHIUSO={"vaso limoni Bancone":("c",L.ad(ban,(-1.75,-0.5)),0.35),"pila Bancone":("c",L.ad(ban,(-1.8,0.6)),0.5)}
closed=[(n,CHIUSO.get(n,sh)) for n,sh in P]
VAR={"vaso limoni Bancone":("c",L.ad(ban,(-2.5,-1.6)),0.35),"pila Bancone":("c",L.ad(ban,(-2.2,0.9)),0.5)}
open_=[(n,VAR.get(n,sh)) for n,sh in P]
c=L.ad(ban,(-1.2,-0.55)); print("varco pila-angolo banco:", round(math.dist(c,VAR["pila Bancone"][1])-0.5,2), "m")
target=L.ad(ban,(0,0.33))
Bc=blocked(closed); Bo=blocked(open_)
for z in L.zones(True):
    if not z["name"].startswith("Pad_"): continue
    dc=path(*Bc,z["pos"],target); do=path(*Bo,z["pos"],target)
    print(f'{z["name"]}: chiuso {dc:.2f} m, varco aperto {do:.2f} m, in più {dc-do:+.2f} m')
