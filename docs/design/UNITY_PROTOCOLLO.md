> **Nota:** documento originale recuperato dal backup del 16/10/2026, con i nomi aggiornati a quelli attuali (Bokobay, Kaia Rio, Sunn Depot, GrayBox…). Può differire da quanto implementato oggi nel gioco.

# Protocollo Unity condiviso (Tech Lead)
- Licenza: una sola Unity Personal attivata sulla macchina condivisa (account di Lorenzo). Nessun altro login. Tutti usano `/home/box/Unity/Hub/Editor/6000.3.26f1/Editor/Unity` in batchmode.
- Unity non può aprire lo stesso progetto due volte. Il progetto principale `/workspace/ultimo-miglio/unity` lo apre SOLO la pipeline del Tech Lead (ci/build.sh e Unity-MCP).
- Per verificare il proprio lavoro (screenshot, compilazione, builder), ognuno usa una copia propria: `git -C /workspace/ultimo-miglio/unity worktree add /workspace/ultimo-miglio/wt/<ruolo> main`, poi Unity con `-projectPath` su quella copia. Mai su `unity/`.
- Consegne: si scrivono i file nella propria copia o direttamente in `unity/Assets/_Game/<area>/` e si avvisa il Tech Lead, che fa il commit e lancia la pipeline. Niente commit diretti su main.
- Lock: prima di lanciare Unity su una copia, controllare `Temp/UnityLockfile`; se il processo è vivo, aspettare.
- Aree: VFX `_Game/VFX`, UI `_Game/UI`, Animator `_Game/Art/Characters`, 3D `_Game/Art` (escluso Characters), Audio `Assets/Audio`, QA solo lettura più `Tests/`.
- Iterazione quotidiana (regola di Lorenzo): NON passa dall'APK. Si usano test EditMode/PlayMode, playthrough automatici in editor e catture in batchmode. L'APK si compila solo per le consegne e per i test di performance sul device, al massimo 1-2 al giorno, con Library e Gradle in cache (build incrementali).
- Lorenzo prova SOLO dal browser: la consegna a lui è la build WebGL (profilo qualità web, WASD/frecce + mouse + joystick virtuale), pubblicata su hosting statico protetto da password. L'APK Android serve a QA e performance.

## Lingua (regola di Lorenzo, 15/10/2026)
Tutta la squadra parla e scrive solo in italiano: messaggi, report, file di stato (anche STATUS.txt), documenti, LEGGIMI e commenti destinati a Lorenzo. Nomi tecnici e codice possono restare come sono.
