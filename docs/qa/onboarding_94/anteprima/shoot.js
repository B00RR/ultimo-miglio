const { chromium } = require('/workspace/shots/node_modules/playwright-core');
const T=JSON.parse(require('fs').readFileSync('testi62.json','utf8')); const S=T.steps,G=T.suggerimenti;
const cases=[
 {k:'01_avvio',scene:'avvio',ada:S[0].text,obj:'Muoviti',hint:G['onb.move.card']},
 {k:'02_comparsa_LN',scene:'reveal',ada:S[4].text,obj:'Raccogli le monete',hint:G['onb.coins.walk']},
 {k:'03_pad_barra',scene:'pad',ada:S[5].text,obj:'Compra lo Zaino',chev:1,price:'18 LN',py:0.45},
 {k:'04_aiuto20_freccia_fuori_camera',scene:'aiuto20',ada:S[1].text,obj:'Vai agli Arrivi',hint:G['onb.pick.max']},
 {k:'05_aiuto20_mano_bersaglio_a_schermo',scene:'aiuto40',ada:S[0].text,obj:'Muoviti',hint:G['onb.move.card']},
 {k:'06_badge_nonno',scene:'nonno',ada:S[6].text,obj:'Assumi NONNO'},
 {k:'08_combo_popup',scene:'combo',ada:S[4].text,obj:'Servi di fila'},
 {k:'07_salta',scene:'salta',ada:S[6].text,obj:'Assumi NONNO'},
];
(async()=>{
 const b=await chromium.launch({executablePath:'/usr/bin/google-chrome'}); const out=[];
 for (const [w,h] of [[360,800],[390,844],[412,915]]) for (const c of cases){
  const p=await b.newPage({viewport:{width:w,height:h},deviceScaleFactor:2});
  const u=new URLSearchParams({scene:c.scene,ada:c.ada,obj:c.obj,...(c.hint?{hint:c.hint}:{}),...(c.chev?{chev:'1',price:c.price,py:String(c.py)}:{})});
  await p.goto('http://127.0.0.1:8854/index.html?'+u); await p.evaluate(()=>document.fonts.ready); await p.waitForTimeout(150);
  const r=await p.evaluate(()=>{const vis=e=>e&&getComputedStyle(e).display!='none'&&e.getClientRects().length;
   const R=id=>{const e=document.getElementById(id);if(!vis(e))return null;let n=e;while(n){if(getComputedStyle(n).display=='none')return null;n=n.parentElement;}const b=e.getBoundingClientRect();return {x:b.left,y:b.top,r:b.right,b:b.bottom}};
   const H=innerHeight,fT=H*350/2532,fB=H-H*500/2532;
   const el={pill:R('currency-pill'),obj:R('obj'),gear:R('gear'),skip:R('skip'),ada:R('ada'),hint:R('hintpill'),joy:R('joystick-base'),hand:R('hand'),chev:R('chev'),bar:R('bar'),price:R('price'),nb:R('nb'),combo:R('combo'),edge:R('edge-arrow')};
   const ov=[];const ks=Object.keys(el).filter(k=>el[k]);const grp=['chev','bar','price'];
   for(let i=0;i<ks.length;i++)for(let j=i+1;j<ks.length;j++){const a=el[ks[i]],c=el[ks[j]];
     if(grp.includes(ks[i])&&grp.includes(ks[j])){ if(a.x<c.r&&c.x<a.r&&a.y<c.b-1&&c.y<a.b-1) ov.push(ks[i]+'×'+ks[j]); continue;}
     if(a.x<c.r&&c.x<a.r&&a.y<c.b&&c.y<a.b)ov.push(ks[i]+'×'+ks[j]);}
   const o=document.getElementById('obj'); const objCut=o.scrollWidth>o.clientWidth+1;
   const top=Math.max(...['pill','obj','gear','skip','ada'].filter(k=>el[k]).map(k=>el[k].b));
   return {ov,objCut,top:Math.round(top),fT:Math.round(fT)};});
  await p.screenshot({path:`onb_${w}_${c.k}.png`}); out.push({w,h,caso:c.k,...r}); await p.close();
 }
 require('fs').writeFileSync('risultati.json',JSON.stringify(out,null,1)); await b.close();
 for(const o of out) console.log(o.w,o.caso,'alto',o.top,'/',o.fT,'objCut',o.objCut,'ov',o.ov.join(','));
})();
