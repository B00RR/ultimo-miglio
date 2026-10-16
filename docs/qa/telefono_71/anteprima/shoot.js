const { chromium } = require('playwright-core');
(async()=>{const b=await chromium.launch({executablePath:'/usr/bin/google-chrome'});
 const cases=[['prima',{v:'prima'}],['dopo',{v:'dopo'}],['dopo_con_rata',{v:'dopo',inst:'Rata 1 · 8.400 LN · scade Giorno 24',quest:'Prossimo obiettivo: abbatti il muro'}]];
 for(const [w,h] of [[360,800],[390,844],[412,915]]) for(const [k,p] of cases){
  const pg=await b.newPage({viewport:{width:w,height:h},deviceScaleFactor:2});
  await pg.goto('http://127.0.0.1:8871/index.html?'+new URLSearchParams(p));await pg.evaluate(()=>document.fonts.ready);await pg.waitForTimeout(100);
  await pg.screenshot({path:`tel_${w}_${k}.png`});await pg.close();}
 await b.close();})();
