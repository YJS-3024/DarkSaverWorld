using DarkSaver.Core;
using DarkSaver.Events;
using UnityEngine;

internal static class ScreenshotConfirmedChecks
{
    public static void Run(Action<bool,string> check)
    {
        var definition=JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/01/Content.json"));
        var boss=definition.units.Single(u=>u.id==700);
        check(boss.hp==300 && boss.CombatHp==450,"source Kisra row preserved; screenshot combat maximum applied");
        check(OriginalEnemyOverrides.MaximumHp(702,300)==300 && OriginalEnemyOverrides.MaximumHp(701,600)==600,"observed Kisra override is not a global multiplier");
        foreach(int hp in new[]{150,300,450})
        {
            var state=Scenario01Progress.Create(definition);state.phase=Scenario01Progress.Playing;state.wave=boss.wave;state.pendingBattle=700;
            var snapshot=new Scenario01EnemyState {id=700,hp=hp,mp=140,x=boss.x,y=boss.y};
            check(Scenario01Rules.RecordBattleReturn(definition,state,700,false,snapshot),"remaining HP snapshot accepted "+hp);
            check(state.Matches(definition) && state.enemies.Single(e=>e.id==700).hp==hp,"old/current remaining HP preserved without refill "+hp);
            var loaded=JsonUtility.FromJson<Scenario01Progress>(JsonUtility.ToJson(state));
            check(loaded.Matches(definition) && loaded.sourceHash==definition.sourceHash && loaded.enemies[0].hp==hp,"unchanged content identity and HP roundtrip "+hp);
        }
        var invalid=Scenario01Progress.Create(definition);invalid.phase=Scenario01Progress.Playing;invalid.wave=boss.wave;invalid.pendingBattle=700;
        check(!Scenario01Rules.RecordBattleReturn(definition,invalid,700,false,new Scenario01EnemyState {id=700,hp=451,mp=140,x=boss.x,y=boss.y})
            && invalid.pendingBattle==700 && invalid.enemies.Count==0,"over-maximum snapshot rejects without mutation");
    }
}
