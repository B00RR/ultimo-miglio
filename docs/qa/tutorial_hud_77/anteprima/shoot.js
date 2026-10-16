const { chromium } = require('/workspace/qa71/node_modules/playwright-core');
const T=JSON.parse(require('fs').readFileSync('/workspace/qa0853/testi62.json','utf8'));
const S=T.steps, G=T.suggerimenti;
const cases=[
 {k:'move',ada:S[0].text,obj:'Muoviti',hint:G['onb.move.card']},
 {k:'pickup',ada:S[1].text,obj:'Vai agli Arrivi',hint:G['onb.pick.max']},
 {k:'shelve',ada:S[2].text,obj:'Metti sullo scaffale',hint:G['onb.deposit.hint']},
 {k:'nonno',ada:S[6].text,obj:'Assumi NONNO',hint:null,chev:1,price:'50 LN',ln:'50',py:0.45},
 {k:'nonno_alto',ada:S[6].text,obj:'Assumi NONNO',hint:null,chev:1,price:'50 LN',ln:'50',py:0.16},
 {k:'fine70',ada:T.fine,obj:'Deposito aperto!',hint:null},
];
(async()=>{
 const b=await chromium.launch({executablePath:'/usr/bin/google-chrome'}); const out=[];
 for (const [w,h] of [[360,800],[390,844],[412,915]]) for (const c of cases){
  const p=await b.newPage({viewport:{width:w,height:h},deviceScaleFactor:2});
  const u=new URLSearchParams({ada:c.ada,obj:c.obj,...(c.hint?{hint:c.hint}:{}),...(c.chev?{chev:'1',price:c.price,ln:c.ln,py:String(c.py)}:{})});
  await p.goto('http://127.0.0.1:8853/index.html?'+u); await p.evaluate(()=>document.fonts.ready); await p.waitForTimeout(200);
  const r=await p.evaluate(()=>{const R=id=>{const e=document.getElementById(id);if(!e||getComputedStyle(e).display=='none'||e.offsetParent===null&&id!='hud-root')return null;const b=e.getBoundingClientRect();return {x:b.left,y:b.top,r:b.right,b:b.bottom}};
   const H=innerHeight,fT=H*350/2532,fB=H-H*500/2532;
   const el={pill:R('currency-pill'),obj:R('obj'),gear:R('gear'),ada:R('ada'),hint:R('hintpill'),joy:R('joystick-base'),poi:R('poi'),chev:R('chev'),price:R('price')};
   const ov=[];const ks=Object.keys(el).filter(k=>el[k]);
   for(let i=0;i<ks.length;i++)for(let j=i+1;j<ks.length;j++){const a=el[ks[i]],c=el[ks[j]];if(['poi','chev','price'].includes(ks[i])&&['poi','chev','price'].includes(ks[j]))continue;
     if(a.x<c.r&&c.x<a.r&&a.y<c.b&&c.y<a.b)ov.push(ks[i]+'×'+ks[j]);}
   if(el.chev&&el.price&&el.chev.b>el.price.y-2) ov.push('freccia×prezzo'); if(el.chev&&el.chev.y<fT) ov.push('freccia in fascia alta');
   const at=document.getElementById('adat'); const lines=Math.round(at.scrollHeight/(14*1.25)); const clipped=at.scrollHeight>at.clientHeight+1;
   const hudTopEnd=Math.max(...['pill','obj','gear','ada'].filter(k=>el[k]).map(k=>el[k].b));
   const hudBotStart=Math.min(...['hint','joy'].filter(k=>el[k]).map(k=>el[k].y));
   const ht=document.getElementById('hintt'); const hintCut=ht.scrollWidth>ht.clientWidth+1;
   return {ov,lines,clipped,hudTopEnd:Math.round(hudTopEnd),fasciaAlta:Math.round(fT),hudBotStart:Math.round(hudBotStart),fasciaBassa:Math.round(fB),hintCut};});
  const f=`hud_${w}_${c.k}.png`; await p.screenshot({path:f}); out.push({w,h,caso:c.k,...r}); await p.close();
 }
 require('fs').writeFileSync('risultati.json',JSON.stringify(out,null,1)); await b.close();
 for(const o of out) console.log(o.w,o.caso,'righe',o.lines,'tagliato',o.clipped,'alto',o.hudTopEnd,'/',o.fasciaAlta,'basso',o.hudBotStart,'/',o.fasciaBassa,'hintCut',o.hintCut,'ov',o.ov.join(','));
})();
