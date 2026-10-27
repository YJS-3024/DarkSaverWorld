using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.Rendering;
using DarkSaver.CameraSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.UI
{
    // Isolated original DEO practice: these units never become owned characters/inventory.
    public sealed partial class FirstPlayTutorial:MonoBehaviour
    {
        private const string TutorialMap="Map_301_;";
        private BattleTransitionController session;
        private TopDownPlayerController player;
        private InGameUi ui;
        private OriginalTutorialScript script;
        private NativeTutorialProgress progress;
        private string error;
        private Camera view;
        private CameraFollow2D follow;
        private bool previousFollow,previousPlayerVisible,ready,leaving;
        private Vector3 previousCamera;
        private float previousSize,delayStarted;
        private AudioSource music;
        private SpriteRenderer playerBody;
        private readonly List<GameObject> hiddenRoots=new();
        private readonly Dictionary<int,GameObject> actors=new();
        private readonly Dictionary<int,Vector3> destinations=new();
        private int displayedStep=-1,openMenu=-1,hovered=-1;
        private string subMenu="";
        private bool movementArmed,attackArmed,magicArmed;
        public static bool BlocksWorldActions=>FindAnyObjectByType<BattleTransitionController>()?.HasPendingTutorial==true;
        public static bool BlocksPointer(Vector2 screenPoint)=>BlocksWorldActions;
        public bool IsReady=>ready;
        public int ScriptCursor=>progress?.step ?? 0;
        public static void Ensure(BattleTransitionController session,TopDownPlayerController player)
        {
            if(session==null || player==null || !session.HasPendingTutorial)return;
            var tutor=session.GetComponent<FirstPlayTutorial>() ?? session.gameObject.AddComponent<FirstPlayTutorial>();
            if(tutor.ready || tutor.script!=null)return;
            tutor.session=session;tutor.player=player;tutor.enabled=true;player.SetTutorialInputBlocked(true);
            if(Application.isPlaying)tutor.StartCoroutine(tutor.Enter());
        }
        private IEnumerator Enter()
        {
            var source=Resources.Load<TextAsset>("Tutorial/TUTORIAL");
            if(source==null){error="원본 튜토리얼 대본을 불러올 수 없습니다.";yield break;}
            script=new OriginalTutorialScript(source.text,session.PlayerJobId);
            if(!Send("Checkpoint"))yield break;
            if(!SceneManager.GetSceneByName(TutorialMap).isLoaded)
            {
                var load=SceneManager.LoadSceneAsync(TutorialMap,LoadSceneMode.Additive);
                if(load==null){error="원본 튜토리얼 맵301을 불러올 수 없습니다.";yield break;}
                yield return load;
            }
            view=Camera.main;if(view==null){error="튜토리얼 카메라를 찾을 수 없습니다.";yield break;}
            previousCamera=view.transform.position;previousSize=view.orthographicSize;
            follow=view.GetComponent<CameraFollow2D>();previousFollow=follow!=null && follow.enabled;if(follow!=null)follow.enabled=false;
            playerBody=player.GetComponent<SpriteRenderer>();if(playerBody!=null){previousPlayerVisible=playerBody.enabled;playerBody.enabled=false;}
            // Preserve existing world scene objects and authored positions rather than rebuilding them.
            foreach(var name in new[]{"Kaosia_Map","Town_Runtime"})
            {
                var scene=SceneManager.GetSceneByName(name);if(!scene.isLoaded)continue;
                foreach(var root in scene.GetRootGameObjects())
                    if(root.activeSelf && root!=player.gameObject && !player.transform.IsChildOf(root.transform)
                        && root!=session.gameObject && !session.transform.IsChildOf(root.transform))
                    {root.SetActive(false);hiddenRoots.Add(root);}
            }
            view.orthographicSize=7.5f;view.transform.position=new Vector3(10,7.5f,previousCamera.z);GameScreen.ApplyCamera(view);
            var clip=Resources.Load<AudioClip>("Scenarios/Shared/Music/nco01");
            if(clip==null){error="원본 튜토리얼 음악 바인딩이 없습니다.";yield break;}
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.spatialBlend=0;music.clip=clip;music.Play();
            SeedPracticeDeaths();ready=true;Reconcile();
        }
        private static Vector3 World(int x,int y)=>new Vector3(x+.5f,15-y-.5f,0);
        private Rect ActorRect(int id)
        {
            if(view==null || !actors.TryGetValue(id,out var actor))return default;
            var center=GameScreen.ScreenToGui(view.WorldToScreenPoint(BattleMapPresentation.GetUnitMenuAnchor(actor.GetComponent<SpriteRenderer>())));
            return new Rect(center.x-16,center.y-16,32,32);
        }
        private bool Send(string kind,int actor=-1,int target=-1,int x=-1,int y=-1,string key=null,int stage=-1,int? expected=null)
        {
            if(session==null)return false;
            int cursor=expected ?? session.CaptureCharacterProfile().tutorial.step;
            if(!session.TryTutorialInput(new TutorialProgressRequest(cursor,kind,actor,target,x,y,key,stage),out error))return false;
            int epoch=progress?.menuEpoch ?? -1;
            progress=session.CaptureCharacterProfile().tutorial.Clone();
            if(progress.menuEpoch!=epoch){openMenu=-1;subMenu="";}
            if(kind=="Checkpoint")
            {
                subMenu=progress.practiceMenu;
                if(!string.IsNullOrEmpty(subMenu))openMenu=0;
                movementArmed=progress.activeCommand=="Move";attackArmed=progress.activeCommand=="Attack";magicArmed=progress.activeCommand=="MagicTarget";
            }
            if(progress.step!=displayedStep){displayedStep=progress.step;delayStarted=Time.unscaledTime;}
            if(kind=="Attack")StartPracticeEffect(actor,target,false);
            if(kind=="Magic" && stage==2)StartPracticeEffect(actor,target,true);
            error=null;
            if(progress.IsComplete && ready && !leaving){leaving=true;StartCoroutine(Leave());}
            return true;
        }
        // Compatibility callers can acknowledge a mouse wait; they cannot skip practice inputs.
        public bool Advance()=>Advance(ScriptCursor);
        public bool Advance(int expectedStep)=>ready && Send("Click",expected:expectedStep);
        private void Update()
        {
            if(!ready || leaving || progress==null || progress.IsComplete || Time.timeScale==0)return;
            movementDebt+=Time.unscaledDeltaTime;
            movementPulse=movementDebt+1e-8>=GameConstants.BattleMoveTickSeconds;
            if(movementPulse)movementDebt=movementDebt>GameConstants.BattleMoveLagResetSeconds?0:Math.Max(0,movementDebt-GameConstants.BattleMoveTickSeconds);
            AnimateActors();TickPracticeEffects();
            var command=script.Current(progress);
            if(command.Name=="DO" && Time.unscaledTime-delayStarted>=int.Parse(command.Arguments[0])/1000f)
            {Send("Delay");return;}
            if(command.Name=="WAITKEYBOARD" && Enum.TryParse(command.Arguments[0],out KeyCode key) && Input.GetKeyDown(key))
            {Send("Key",key:command.Arguments[0]);return;}
            if(view==null || !GameScreen.TryScreenToGui(Input.mousePosition,out var pointer))return;
            hovered=-1;foreach(var a in progress.actors)if(!a.removed && !a.dead && ActorRect(a.id).Contains(pointer)){hovered=a.id;break;}
            if(command.Name=="WAITMOUSEMOVE" && hovered>=0){Send("Hover",actor:hovered);return;}
            if(Input.GetMouseButtonDown(1)){openMenu=-1;subMenu="";movementArmed=attackArmed=magicArmed=false;return;}
            if(!Input.GetMouseButtonDown(0) || UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject()==true)return;
            if(command.Name=="WAITMOUSEBUTTON"){Send("Click");return;}
            if(!progress.mouseMenu)return;
            if(hovered==0 || hovered==1)
            {
                openMenu=hovered;subMenu="";
                if(command.Name=="WAITTOPMENU")Send("TopMenu",actor:hovered);
                return;
            }
            if(command.Name=="WAITMOVEPOS")
            {
                if(!movementArmed)return;
                var pos=view.ScreenToWorldPoint(Input.mousePosition);int x=Mathf.FloorToInt(pos.x),y=14-Mathf.FloorToInt(pos.y);
                if(x==int.Parse(command.Arguments[1]) && y==int.Parse(command.Arguments[2]))
                    BeginPlayerMovement(int.Parse(command.Arguments[0]),x,y);
                return;
            }
            if(hovered>=0 && (command.Name=="WAITATTACKCHAR" || command.Name=="WAITMAGICMENU" && command.Arguments[0]=="2"))
            {
                if(command.Name=="WAITATTACKCHAR" && attackArmed){if(Send("Attack",actor:0,target:hovered))attackArmed=false;}
                else if(command.Name=="WAITMAGICMENU" && magicArmed){if(Send("Magic",actor:0,target:hovered,stage:2))magicArmed=false;}
                openMenu=-1;subMenu="";return;
            }

        }
        private int playerMoving=-1,moveX,moveY;
        private Vector3 moveDestination;
        private void BeginPlayerMovement(int id,int x,int y)
        {
            if(playerMoving>=0 || !actors.ContainsKey(id))return;
            playerMoving=id;moveX=x;moveY=y;moveDestination=World(x,y);
        }
        private void AnimateActors()
        {
            // Native 80ms presentation ticks; no battle action cost or movement authority is inferred.
            foreach(var a in progress.actors.Where(a=>a.moving).ToArray())
            {
                if(!actors.TryGetValue(a.id,out var actor))continue;
                var end=World(a.targetX,a.targetY);MoveView(actor,end);
                if((actor.transform.position-end).sqrMagnitude<.0001f)Send("MovementCompleted",actor:a.id,x:a.targetX,y:a.targetY);
            }
            if(playerMoving>=0 && actors.TryGetValue(playerMoving,out var playerView))
            {
                MoveView(playerView,moveDestination);
                if((playerView.transform.position-moveDestination).sqrMagnitude<.0001f)
                {int id=playerMoving;playerMoving=-1;if(Send("MovePosition",actor:id,x:moveX,y:moveY))movementArmed=false;openMenu=-1;}
            }
        }
        private sealed class PracticeMotion
        {public Vector3 from,next,end;public int phase;}
        private readonly Dictionary<GameObject,PracticeMotion> motions=new();
        private double movementDebt;
        private bool movementPulse;
        private void MoveView(GameObject actor,Vector3 end)
        {
            if(!motions.TryGetValue(actor,out var motion) || motion.end!=end)
            {motion=new PracticeMotion {from=actor.transform.position,end=end};motion.next=NextPracticeCell(motion.from,end);motions[actor]=motion;}
            var delta=motion.next-motion.from;bool diagonal=Math.Abs(delta.x)>.001f && Math.Abs(delta.y)>.001f;
            var animator=actor.GetComponent<DirectionalSpriteAnimator>();animator.TrySetFacing(new Vector2(delta.x,delta.y));animator.SetMovingPresentation(true);
            if(movementPulse)
            {
                motion.phase++;
                int phases=diagonal?GameConstants.BattleDiagonalMoveTicks:GameConstants.BattleCardinalMoveTicks;
                if(motion.phase>=phases)
                {
                    actor.transform.position=motion.next;motion.from=motion.next;motion.phase=0;
                    if((motion.from-end).sqrMagnitude<.0001f){motions.Remove(actor);animator.SetMovingPresentation(false);}
                    else motion.next=NextPracticeCell(motion.from,end);
                }
                else
                {
                    float fraction=motion.phase*(diagonal?GameConstants.BattleDiagonalMoveTickPixels:GameConstants.BattleCardinalMoveTickPixels)/(float)GameConstants.BattleMoveSourceTilePixels;
                    actor.transform.position=Vector3.Lerp(motion.from,motion.next,fraction);
                }
            }
            actor.GetComponent<SpriteRenderer>().sortingOrder=20000-Mathf.RoundToInt(actor.transform.position.y*10);
        }
        // Tutorial destinations are source-scripted. General native terrain path selection remains separate.
        private static Vector3 NextPracticeCell(Vector3 from,Vector3 end)=>from+new Vector3(Mathf.Clamp(end.x-from.x,-1,1),Mathf.Clamp(end.y-from.y,-1,1),0);
        private void Reconcile()
        {
            if(progress==null)return;
            foreach(var a in progress.actors)
            {
                if(!actors.TryGetValue(a.id,out var actor))
                {
                    actor=new GameObject("Tutorial actor "+a.id,typeof(SpriteRenderer),typeof(DirectionalSpriteAnimator));
                    actor.transform.SetParent(transform,false);actor.transform.position=World(a.x,a.y);actors.Add(a.id,actor);
                }
                actor.SetActive(!a.removed && (!a.dead || IsPracticeCorpseVisible(a.id)));
                actor.GetComponent<SpriteRenderer>().enabled=true;
                int color=a.id==100?(progress.ally==0?3:2):0;
                if(!destinations.ContainsKey(a.id) || destinations[a.id].z!=color)
                {
                    Sprite[] Frames(string direction)=>Enumerable.Range(0,3).Select(i=>Resources.Load<Sprite>($"Tutorial/Characters/{a.job}_{color}_{direction}_{i}")).ToArray();
                    var up=Frames("Up");var down=Frames("Down");var left=Frames("Left");var right=Frames("Right");
                    if(up.Concat(down).Concat(left).Concat(right).Any(s=>s==null)){error="원본 튜토리얼 캐릭터 그림이 없습니다: "+a.job;return;}
                    actor.GetComponent<DirectionalSpriteAnimator>().Configure(up,down,left,right);destinations[a.id]=new Vector3(0,0,color);
                }
                if(!a.moving && playerMoving!=a.id)
                {
                    actor.transform.position=World(a.x,a.y);var anim=actor.GetComponent<DirectionalSpriteAnimator>();anim.SetMovingPresentation(false);
                    anim.TrySetFacing(a.facing=="UP"?Vector2.up:a.facing=="LEFT"?Vector2.left:a.facing=="RIGHT"?Vector2.right:Vector2.down);anim.PresentFrame(0);
                }
                actor.GetComponent<SpriteRenderer>().sortingOrder=20000-Mathf.RoundToInt(actor.transform.position.y*10);
            }
        }
        private IEnumerator Leave()
        {
            ready=false;ui?.Hide();RestoreWorld();
            if(SceneManager.GetSceneByName(TutorialMap).isLoaded)yield return SceneManager.UnloadSceneAsync(TutorialMap);
            player?.SetTutorialInputBlocked(false);enabled=false;
        }
        private void RestoreWorld()
        {
            ReleaseRanges();ClearPracticeEffects();motions.Clear();
            if(minimapCamera!=null){Destroy(minimapCamera.gameObject);minimapCamera=null;}
            if(minimap!=null){minimap.Release();Destroy(minimap);minimap=null;}
            if(music!=null){music.Stop();Destroy(music);music=null;}
            foreach(var actor in actors.Values)if(actor!=null)Destroy(actor);actors.Clear();destinations.Clear();
            foreach(var root in hiddenRoots)if(root!=null)root.SetActive(true);hiddenRoots.Clear();
            if(playerBody!=null)playerBody.enabled=previousPlayerVisible;
            if(view!=null){view.transform.position=previousCamera;view.orthographicSize=previousSize;}
            if(follow!=null)follow.enabled=previousFollow;
        }
        private void OnDisable(){ui?.Hide();if(Application.isPlaying)RestoreWorld();player?.SetTutorialInputBlocked(session?.HasPendingTutorial==true);}
    }
}
