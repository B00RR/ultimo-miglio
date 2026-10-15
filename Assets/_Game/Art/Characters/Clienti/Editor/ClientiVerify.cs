// Verifica clienti v02 in editor (batchmode): avatar validi, Animator muove le ossa, controller risolto.
using UnityEditor; using UnityEngine; using System.Linq;
public static class ClientiVerify {
    const string D="Assets/_Game/Art/Characters/Clienti/";
    static bool Is(Animator a,string s)=>a.GetCurrentAnimatorStateInfo(0).IsName(s)||a.GetNextAnimatorStateInfo(0).IsName(s);
    public static void Run() {
        int fail=0;
        var ctrl=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(D+"chr_cliente.controller");
        if(ctrl==null){Debug.LogError("UMV controller NON risolto");EditorApplication.Exit(1);return;}
        var ac=ctrl as UnityEditor.Animations.AnimatorController;
        Debug.Log("UMV controller ok clip="+string.Join(",",ctrl.animationClips.Where(c=>c!=null).Select(c=>c.name).Distinct())+" nulli="+ctrl.animationClips.Count(c=>c==null)+" param="+string.Join(",",ac.parameters.Select(p=>p.name+":"+p.type)));
        foreach(var n in new[]{"base","residente","rider","anziana","turista","vip"}){
            string p=D+"chr_cliente_"+n+".fbx";
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var av=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Avatar>().FirstOrDefault();
            var clips=AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).Select(c=>c.name+(c.isHumanMotion?"(H)":"")+(c.isLooping?"L":""));
            if(go==null||av==null||!av.isValid||!av.isHuman){Debug.LogError("UMV "+n+" avatar NON valido");fail++;continue;}
            var inst=(GameObject)Object.Instantiate(go); var an=inst.GetComponent<Animator>()??inst.AddComponent<Animator>();
            an.avatar=av; an.runtimeAnimatorController=ctrl; an.applyRootMotion=false; an.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var hand=an.GetBoneTransform(HumanBodyBones.RightHand); var foot=an.GetBoneTransform(HumanBodyBones.LeftFoot);
            an.Rebind(); an.SetFloat("Speed",1.2f); an.SetFloat("Patience",1f); an.Update(0f); an.Update(0.25f);
            Vector3 f1=foot.position,h1=hand.position; an.Update(0.2f); Vector3 f2=foot.position,h2=hand.position;
            string st=an.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")?"Locomotion":"?";
            an.SetFloat("Speed",0);an.SetFloat("Patience",0.1f);an.Update(0.5f);an.Update(0.5f);
            bool imp=an.GetCurrentAnimatorStateInfo(0).IsName("Wait_Impatient");
            an.SetFloat("Patience",1f);for(int i=0;i<6;i++)an.Update(0.1f);an.SetTrigger("Pay");bool pay=false;for(int i=0;i<8;i++){an.Update(0.1f);pay|=Is(an,"Pay");}
            for(int i=0;i<60;i++)an.Update(0.1f);an.SetTrigger("Angry");bool ang=false;for(int i=0;i<8;i++){an.Update(0.1f);ang|=Is(an,"Angry");}
            float d=(f2-f1).magnitude+(h2-h1).magnitude; bool mv=d>1e-3;
            var smr=inst.GetComponentInChildren<SkinnedMeshRenderer>();
            Debug.Log($"UMV {n} avatarValido={av.isValid} human={av.isHuman} stato={st} piedeΔ={(f2-f1)} manoΔ={(h2-h1).magnitude:F3} muove={mv} impaziente={imp} pay={pay} angry={ang} ossa={smr.bones.Length} mat={string.Join(",",smr.sharedMaterials.Select(m=>m?m.name:"null"))} clip=[{string.Join(",",clips)}]");
            if(!mv||!imp||!pay||!ang) fail++;
            Object.DestroyImmediate(inst);
        }
        Debug.Log("UMV FINE fail="+fail); EditorApplication.Exit(fail==0?0:1);
    }
}
