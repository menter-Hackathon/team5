using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BandProgressionCheck
{
    static BandProgressionCheck() { EditorApplication.playModeStateChanged += Mode; }
    static void Require(bool ok,string message) { if(!ok) throw new Exception(message); }
    static object Call(object target,string method) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static T Load<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/"+path);
    public static void Run()
    {
        try {
            var role=Load<RoleDefinition>("Data/Roles/Role_BandMan.asset");
            Require(role.recipes.Count==3,"Band must have three stages");
            var ids=new[]{"sheet_music","performance_setup","original_song"};
            var inputs=new[]{new[]{"paper","pencil"},new[]{"tuner","amplifier"},new[]{"sheet_music","performance_setup"}};
            for(int i=0;i<3;i++) {
                var r=role.recipes[i];Require(r.recipeId==ids[i],"Wrong recipe order");
                Require(r.inputs.Select(x=>x.item.itemId).SequenceEqual(inputs[i]),"Wrong materials "+ids[i]);
                Require(r.inputs.All(x=>x.count==1),"Wrong input counts");
            }
            Require(role.goalItem==role.recipes[2].output,"Goal reference");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity");
            var pickups=UnityEngine.Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
            foreach(var id in new[]{"paper","pencil","tuner","amplifier"}) {
                var matches=pickups.Where(x=>x.Item!=null && x.Item.itemId==id && x.gameObject.activeInHierarchy).ToArray();
                Require(matches.Sum(x=>x.Count)>=1,"Unobtainable material "+id);
                foreach(var p in matches.Where(x=>x.Item.itemId!="paper")) {
                    Require(p.GetComponent<Collider>()!=null,"No pickup collider");
                    Require(p.GetComponentsInChildren<Renderer>().Any(x=>x.enabled),"No pickup visual");
                    Physics.SyncTransforms();
                    var blocked=Physics.OverlapSphere(p.transform.position,0.15f,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(p.transform)).ToArray();
                    Require(blocked.Length==0,"Pickup inside obstacle: "+id+" "+string.Join(",",blocked.Select(c=>c.name)));
                    Debug.Log("BAND_PICKUP "+id+" "+p.transform.position);
                }
            }
            // Every role must still have an acyclic recipe graph and obtainable inputs.
            foreach(var g in AssetDatabase.FindAssets("t:RoleDefinition",new[]{"Assets/_Project/Data/Roles"})) {
                var other=AssetDatabase.LoadAssetAtPath<RoleDefinition>(AssetDatabase.GUIDToAssetPath(g));
                if(other.roleId=="sample")continue;
                var available=new HashSet<ItemDefinition>(pickups.Where(x=>x.Item!=null).Select(x=>x.Item));
                foreach(var t in other.imposedTasks) if(t!=null && t.rewardItem!=null)available.Add(t.rewardItem);
                var pending=new List<RecipeDefinition>(other.recipes);
                while(pending.Count>0) {
                    var ready=pending.FirstOrDefault(x=>x!=null && x.inputs.All(a=>available.Contains(a.item)));
                    Require(ready!=null,"Unreachable recipe for "+other.roleId);
                    if(ready.output!=null)available.Add(ready.output); pending.Remove(ready);
                }
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SessionState.SetBool("BandCheck",true);EditorApplication.EnterPlaymode();
        } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
    static void Mode(PlayModeStateChange mode) { if(mode==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BandCheck",false))EditorApplication.delayCall+=Check; }
    static void Check()
    {
        try {
            var ctx=new GameObject("Context").AddComponent<GameSceneContext>();ctx.enabled=false;
            ctx.Balance=Load<GameBalance>("Data/GameBalance.asset");
            ctx.Inventory=new GameObject("Inventory").AddComponent<Inventory>();
            ctx.Parameters=new GameObject("Parameters").AddComponent<PlayerParameters>();ctx.Parameters.enabled=false;
            ctx.Timer=new GameObject("Timer").AddComponent<GameTimer>();ctx.Timer.enabled=false;
            ctx.Crafting=new GameObject("Crafting").AddComponent<CraftingSystem>();ctx.Crafting.enabled=false;
            var judge=new GameObject("Judge").AddComponent<GameEndJudge>();judge.enabled=false;
            var endings=new List<GameResult>();judge.OnEnding+=r=>endings.Add(r);Call(judge,"Start");
            var role=Load<RoleDefinition>("Data/Roles/Role_BandMan.asset");
            var craft=ctx.Crafting;var inv=ctx.Inventory;
            Require(!craft.CanCraft(role.recipes[2]),"Final craft allowed without preparation");
            foreach(var recipe in role.recipes.Take(2))foreach(var input in recipe.inputs)inv.Add(input.item,input.count);
            craft.StartCraft(role.recipes[0]);craft.Cancel();
            Require(inv.Has(role.recipes[0].inputs) && ctx.Parameters.Get(ParameterType.RoleProgress)==0,"Cancel consumes materials/progress");
            int[] progress={30,60,100};
            for(int i=0;i<3;i++) {
                Require(craft.CanCraft(role.recipes[i]),"Cannot craft stage "+i);
                craft.StartCraft(role.recipes[i]);Call(craft,"Finish");
                Require(ctx.Parameters.Get(ParameterType.RoleProgress)==progress[i],"Wrong stage progress");
                Require(role.recipes[i].inputs.All(x=>inv.Count(x.item)==0),"Materials not consumed");
                Require(!craft.CanCraft(role.recipes[i]),"Stage repeat without materials");
                Require(endings.Count==(i==2?1:0),"Premature or missing ending");
            }
            Require(endings[0]==GameResult.Clear && inv.Count(role.goalItem)==1,"No clear/goal");
            Debug.Log("BAND_CHECK_ALL_PASS: scene pickups, all-role recipe reachability, exact 3-stage inputs, cancellation, consumption, 30/60/100 progress and final-only clear");
            SessionState.SetBool("BandCheck",false);EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e);SessionState.SetBool("BandCheck",false);EditorApplication.Exit(1); }
    }
}
