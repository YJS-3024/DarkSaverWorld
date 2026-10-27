using DarkSaver.Combat;
using DarkSaver.Jobs;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Tests
{
    public sealed class BattleFeedbackTests
    {
        [Test]
        public void RemainingMagicVisualDataMatchesNativePlasmaMeteorAndFlashRules()
        {
            var plasma=Resources.Load<NativeThrustBeamEffects>("Effects/NativePlasmaBeamEffects");Assert.IsNotNull(plasma);Assert.AreEqual(9,plasma.PartCount);
            for(int i=0;i<5;i++){Assert.IsTrue(plasma.TryGetLifetime(10501+i,out int life));Assert.AreEqual((i+1)*5,life);}
            for(int dir=-1;dir>=-4;dir--)
            {int count=0;for(int part=0;part<9;part++)if(plasma.TryGetPart(10505,0,dir,part,out _,out _,out _))count++;Assert.AreEqual(dir==-1?3:9,count);}
            Assert.IsTrue(plasma.TryGetPart(10505,20,-2,4,out _,out _,out var size));Assert.AreEqual(new Vector2(170,22),size); // Side component's own Lv1 duration must not truncate plasma.
            int[] countByPost={4,3,2,1,7,6,5,4,3};for(int i=0;i<9;i++)Assert.AreEqual(countByPost[i],BattleMapPresentation.NativeMeteorCount(i+1));
            Assert.AreEqual(new Vector2(48,118),BattleMapPresentation.NativeMeteorOffset(1,0,0,0,0));
            Assert.AreEqual(0,BattleMapPresentation.NativeMeteorCount(10));
            for(int i=0;i<=24;i++)Assert.AreEqual(i==14 || i==16 || i==18 || i==20 || i==22,NativeLevelUpCircleEffects.IsFlashCounter(i));
            var sounds=Resources.Load<NativeMagicSoundCatalog>("Effects/NativeMagicSoundCatalog");Assert.IsNotNull(sounds);Assert.AreEqual(16,sounds.Count);
            foreach(int id in new[]{2,3,5,6,7,8,9,12,15,17,18,19,20,21,22,24})
            {var clip=sounds.Get(id);Assert.IsNotNull(clip);Assert.AreEqual(1,clip.channels);Assert.AreEqual(22050,clip.frequency);Assert.Greater(clip.samples,0);}
            Assert.IsNull(sounds.Get(0));
            var meteors=Resources.Load<NativeTargetEffects>("Effects/NativeMeteorTargetEffects");Assert.IsNotNull(meteors);
            Assert.IsTrue(meteors.TryGet(1001,out var meteor));Assert.IsTrue(meteor.RandomGround);Assert.AreEqual(24,meteor.LifetimeCounters);
            var quake=Resources.Load<NativeTargetEffects>("Effects/NativeCoordinateTargetEffects");Assert.AreEqual(1,quake.PatternCount);
            Assert.IsTrue(quake.TryGet(901,out var quakeTarget));Assert.AreEqual(10,quakeTarget.LifetimeCounters);Assert.IsTrue(quakeTarget.CenterOnUnit);
        }

        [Test]
        public void DoubleThrustUsesTwoOffsetArmsAndPreservesNativeEmptyRightRectangles()
        {
            var catalog=Resources.Load<NativeThrustBeamEffects>("Effects/NativeDoubleThrustBeamEffects");
            Assert.IsNotNull(catalog);Assert.IsTrue(catalog.DoubleArms);Assert.AreEqual(6,catalog.PartCount);
            int[] life={10,15,15,20,25,30};
            for(int i=0;i<6;i++){Assert.IsTrue(catalog.TryGetLifetime(10301+i,out int value));Assert.AreEqual(life[i],value);}
            for(int i=0;i<6;i++)Assert.IsFalse(catalog.TryGetPart(10301,0,-1,i,out _,out _,out _));
            Assert.IsFalse(catalog.TryGetPart(10301,0,0,0,out _,out _,out _));
            Assert.IsTrue(catalog.TryGetPart(10301,5,-2,1,out _,out var first,out var size));Assert.AreEqual(new Vector2(-48,-32),first);Assert.AreEqual(new Vector2(68,22),size);
            Assert.IsTrue(catalog.TryGetPart(10301,5,-2,4,out _,out var second,out _));Assert.AreEqual(new Vector2(0,64),second-first);
            Assert.IsTrue(catalog.TryGetPart(10301,0,-3,0,out _,out first,out _));Assert.AreEqual(new Vector2(-32,11),first);
            Assert.IsTrue(catalog.TryGetPart(10301,0,-4,5,out _,out second,out _));Assert.AreEqual(new Vector2(32,-54),second);
            Assert.IsFalse(catalog.TryGetPart(10301,10,-2,0,out _,out _,out _));
        }

        [Test]
        public void ExpandingCasterUsesOriginalModuloIntegerStretchAndAreaLifetime()
        {
            var catalog=Resources.Load<NativeExpandingCasterEffects>("Effects/NativeExpandingCasterEffects");
            Assert.IsNotNull(catalog);Assert.AreEqual(9,catalog.MagicCount);
            int[] durations={13,22,31,40,49,64};
            for(int i=0;i<6;i++){Assert.IsTrue(catalog.TryGet(10401+i,out var e));Assert.AreEqual(durations[i],e.Lifetime);Assert.IsNull(e.Frame(e.Lifetime));}
            Assert.IsTrue(catalog.TryGet(10401,out var circle));Assert.AreEqual(new Vector2(38,38),circle.PixelSize(0));
            Assert.AreEqual(new Vector2(44,44),circle.PixelSize(1));Assert.AreEqual(new Vector2(52,52),circle.PixelSize(2));
            Assert.AreSame(circle.Frame(0),circle.Frame(3));Assert.IsNull(circle.Frame(-1));
            Assert.IsTrue(catalog.TryGet(10406,out var skull));Assert.AreEqual(4,skull.FrameCount);
            Assert.AreEqual(new Vector2(10,26),skull.PixelSize(0));Assert.AreEqual(new Vector2(18,34),skull.PixelSize(1));
            for(int id=6601;id<=6603;id++){Assert.IsTrue(catalog.TryGet(id,out var red));Assert.AreEqual(49,red.Lifetime);Assert.AreEqual(new Vector2(48,48),red.PixelSize(0));}
            Assert.IsFalse(catalog.TryGet(10407,out _));
        }

        [Test]
        public void GroundBurstCasterUsesCheckerMaskPerCellJitterAndNoPause()
        {
            var catalog=Resources.Load<NativeGroundBurstCasterEffects>("Effects/NativeGroundBurstCasterEffects");
            Assert.IsNotNull(catalog);Assert.AreEqual(6,catalog.MagicCount);int[] areas={2,3,3,4,4,4};
            for(int i=0;i<6;i++)
            {
                Assert.IsTrue(catalog.TryGet(10601+i,out int area,out var pattern));Assert.AreEqual(areas[i],area);Assert.AreEqual(24,pattern.LifetimeCounters);
                var values=new long[16];values[0]=10601+i;values[2]=-5;values[3]=area;
                var magic=new DarkSaver.Magic.MagicCatalogEntry(values,"Mask regression");int count=0;
                for(int x=-6;x<=6;x++)for(int y=-6;y<=6;y++)
                {
                    bool inside=NativeGroundBurstCasterEffects.Contains(area,x,y);if(inside)count++;
                    Assert.IsTrue(CombatMagicArea.TryGetWeight(magic,new CombatGridCell(8,8),default,new CombatGridCell(8+x,8+y),20,20,out int weight));
                    Assert.AreEqual(weight>0,inside);
                }
                Assert.AreEqual((area+1)*(area+1),count);
                Assert.IsNull(NativeGroundBurstCasterEffects.Frame(pattern,0,0));
                Assert.AreSame(pattern.Frame(0),NativeGroundBurstCasterEffects.Frame(pattern,0,1));
                Assert.AreSame(pattern.Frame(2),NativeGroundBurstCasterEffects.Frame(pattern,0,2));
                Assert.IsNull(NativeGroundBurstCasterEffects.Frame(pattern,24,1));
            }
            Assert.AreEqual(5,NativeGroundBurstCasterEffects.NextCounter(0,4));
            Assert.AreEqual(2,NativeGroundBurstCasterEffects.NextCounter(1,4));
            Assert.IsFalse(catalog.TryGet(10101,out _,out _));
        }

        [Test]
        public void ThrustBeamUsesOriginalCapsSeventeenStripsAndFiveCounterGrowth()
        {
            var catalog=Resources.Load<NativeThrustBeamEffects>("Effects/NativeThrustBeamEffects");
            Assert.IsNotNull(catalog);Assert.AreEqual(6,catalog.MagicCount);
            for(int i=0;i<6;i++){Assert.IsTrue(catalog.TryGetLifetime(10101+i,out int life));Assert.AreEqual((i+2)*5,life);}
            Assert.IsFalse(catalog.TryGetLifetime(1401,out _));Assert.IsFalse(catalog.TryGetLifetime(18101,out _));
            Assert.IsTrue(catalog.TryGetPart(10101,0,-1,0,out var start,out var offset,out var size));
            Assert.AreEqual("Beam_0",start.name);Assert.AreEqual(new Vector2(10,0),offset);Assert.AreEqual(new Vector2(10,22),size);
            Assert.IsTrue(catalog.TryGetPart(10101,4,-1,1,out _,out offset,out size));Assert.AreEqual(new Vector2(32,0),offset);Assert.AreEqual(new Vector2(34,22),size);
            Assert.IsTrue(catalog.TryGetPart(10101,5,-1,1,out _,out offset,out size));Assert.AreEqual(new Vector2(49,0),offset);Assert.AreEqual(new Vector2(68,22),size);
            Assert.IsTrue(catalog.TryGetPart(10101,0,-2,2,out var end,out offset,out _));Assert.AreSame(start,end);Assert.AreEqual(new Vector2(-45,0),offset);
            Assert.IsTrue(catalog.TryGetPart(10101,0,-3,1,out _,out offset,out size));Assert.AreEqual(new Vector2(0,31),offset);Assert.AreEqual(new Vector2(22,34),size);
            Assert.IsTrue(catalog.TryGetPart(10101,0,-4,2,out _,out offset,out _));Assert.AreEqual(new Vector2(0,-54),offset);
            Assert.IsFalse(catalog.TryGetPart(10101,10,-1,0,out _,out _,out _));
            Assert.IsFalse(catalog.TryGetPart(10101,-1,-1,0,out _,out _,out _));
        }

        [Test]
        public void SalamanderUsesTruncatedStepThreeDirectionsAndAreaLifetime()
        {
            var catalog=Resources.Load<NativeSalamanderEffects>("Effects/NativeSalamanderEffects");
            Assert.IsNotNull(catalog);Assert.AreEqual(6,catalog.MagicCount);
            int[] life={6,6,8,8,10,12};
            for(int i=0;i<6;i++){Assert.IsTrue(catalog.TryGetLifetime(10701+i,out int duration));Assert.AreEqual(life[i],duration);}
            var delta=new Vector2Int(32,-32);
            Assert.IsTrue(catalog.TryGetPiece(10701,0,delta,4,out var right,out var position));
            Assert.AreEqual("Salamander_0",right.name);Assert.AreEqual(new Vector2(9.5f,10),position);
            Assert.IsTrue(catalog.TryGetPiece(10701,1,delta,4,out var alternate,out _));Assert.AreEqual("Salamander_1",alternate.name);
            Assert.IsTrue(catalog.TryGetPiece(10701,0,new Vector2Int(-32,-32),4,out var left,out _));Assert.AreEqual("Salamander_2",left.name);
            Assert.IsTrue(catalog.TryGetPiece(10701,0,new Vector2Int(0,-32),4,out var up,out _));Assert.AreEqual("Salamander_4",up.name);
            Assert.IsTrue(catalog.TryGetPiece(10701,0,new Vector2Int(0,32),4,out var down,out _));Assert.AreSame(left,down);
            Assert.IsFalse(catalog.TryGetPiece(10701,0,delta,3,out _,out _));
            Assert.IsTrue(catalog.TryGetPiece(10701,1,delta,3,out _,out _));
            Assert.IsFalse(catalog.TryGetPiece(10701,6,delta,4,out _,out _));
            Assert.IsFalse(catalog.TryGetPiece(10701,-1,delta,4,out _,out _));
            Assert.IsFalse(catalog.TryGetPiece(10201,0,delta,4,out _,out _));
        }

        [Test]
        public void LevelUpCircleAccumulatesTwelveGroundPiecesUntilCounterTwentyTwo()
        {
            var catalog=Resources.Load<NativeLevelUpCircleEffects>("Effects/NativeLevelUpCircleEffects");
            Assert.IsNotNull(catalog);Assert.IsTrue(catalog.Supports(4001));Assert.IsFalse(catalog.Supports(10201));
            Assert.AreEqual(0,NativeLevelUpCircleEffects.VisibleCount(0));
            for(int i=1;i<22;i++)Assert.AreEqual(System.Math.Min(i,12),NativeLevelUpCircleEffects.VisibleCount(i));
            Assert.AreEqual(0,NativeLevelUpCircleEffects.VisibleCount(22));Assert.AreEqual(0,NativeLevelUpCircleEffects.VisibleCount(-1));
            for(int i=0;i<12;i++)Assert.IsNotNull(catalog.Piece(i));
            Assert.IsNull(catalog.Piece(12));Assert.AreEqual(new Vector2(38,32.5f),catalog.Offset(0));
            Assert.AreEqual(new Vector2(37,6.5f),catalog.Offset(11));
        }

        [Test]
        public void CrossTrustUsesOriginalFourArmsTrailingWindowAndAreaLifetime()
        {
            var catalog = Resources.Load<NativeCrossTrustEffects>("Effects/NativeCrossTrustEffects");
            Assert.IsNotNull(catalog); Assert.AreEqual(5,catalog.MagicCount);
            for(int i=0;i<5;i++) { Assert.IsTrue(catalog.TryGetLifetime(10201+i,out var life)); Assert.AreEqual((i+2)*5,life); }
            Assert.IsFalse(catalog.TryGetLifetime(10101,out _));
            Assert.IsTrue(catalog.TryGetSegment(0,0,0,out var first,out var pixels));
            Assert.AreSame(catalog.Frame(0),first); Assert.AreEqual(new Vector2(-.5f,.5f),pixels);
            Assert.IsFalse(catalog.TryGetSegment(0,2,0,out _,out _));
            Assert.IsFalse(catalog.TryGetSegment(0,0,1,out _,out _));
            int count=0;
            for(int d=0;d<4;d++)for(int slot=0;slot<4;slot++)if(catalog.TryGetSegment(3,d,slot,out _,out _))count++;
            Assert.AreEqual(14,count);
            Assert.IsTrue(catalog.TryGetSegment(3,0,3,out var head,out var right));
            Assert.AreSame(catalog.Frame(0),head); Assert.AreEqual(new Vector2(20.5f,.5f),right);
            Assert.IsTrue(catalog.TryGetSegment(3,1,3,out _,out var left)); Assert.AreEqual(new Vector2(-21.5f,.5f),left);
            Assert.IsFalse(catalog.TryGetSegment(-1,0,0,out _,out _));
        }

        [Test]
        public void GroundExplosionUsesOriginalRandomStartPauseAndHalfSpeedFrames()
        {
            var catalog = Resources.Load<NativeTargetEffects>("Effects/NativeGroundExplosionEffects");
            Assert.IsNotNull(catalog); Assert.AreEqual(2, catalog.PatternCount); Assert.AreEqual(6, catalog.MagicCount);
            Assert.IsTrue(catalog.TryGet(10601, out var pattern)); Assert.AreEqual(24, pattern.LifetimeCounters);
            Assert.IsTrue(pattern.RandomGround); Assert.IsFalse(pattern.CenterOnUnit);
            for (int i = 0; i < 24; i++) StringAssert.EndsWith("_" + (i / 2 % 5), pattern.Frame(i).name);
            Assert.IsNull(pattern.Frame(24)); Assert.IsNull(pattern.Frame(-1));
            Assert.AreEqual(4, NativeTargetEffects.NextGroundCounter(0, 4, 0));
            Assert.AreEqual(5, NativeTargetEffects.NextGroundCounter(0, 4, 1));
            Assert.AreEqual(7, NativeTargetEffects.NextGroundCounter(7, 4, 0));
            Assert.AreEqual(8, NativeTargetEffects.NextGroundCounter(7, 0, 2));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => NativeTargetEffects.NextGroundCounter(0, 5, 0));
        }

        [Test]
        public void DelayedTargetWaitsElevenCountersAndUsesAbsoluteCounterModulo()
        {
            var catalog = Resources.Load<NativeTargetEffects>("Effects/NativeDelayedTargetEffects");
            Assert.IsNotNull(catalog); Assert.AreEqual(8, catalog.PatternCount); Assert.AreEqual(40, catalog.MagicCount);
            foreach (int id in new[] { 10101, 10701 })
            {
                Assert.IsTrue(catalog.TryGet(id, out var pattern)); Assert.AreEqual(25, pattern.LifetimeCounters);
                Assert.IsTrue(pattern.CenterOnUnit);
                for (int i = 0; i < 11; i++) Assert.IsNull(pattern.Frame(i));
                for (int i = 11; i < 25; i++)
                {
                    Assert.IsNotNull(pattern.Frame(i)); Assert.AreSame(pattern.Frame(11 + (i - 11) % pattern.FrameCount), pattern.Frame(i));
                }
                Assert.IsNull(pattern.Frame(25)); Assert.IsNull(pattern.Frame(-1));
            }
            Assert.IsTrue(catalog.TryGet(10101, out var trust));
            StringAssert.EndsWith("_3", trust.Frame(11).name); StringAssert.EndsWith("_0", trust.Frame(24).name);
            Assert.IsTrue(catalog.TryGet(10701, out var salamander));
            StringAssert.EndsWith("_1", salamander.Frame(11).name); StringAssert.EndsWith("_4", salamander.Frame(24).name);
        }

        [Test]
        public void CurvedDirectionalProjectileKeepsSingleStepCountersAndChangesFacing()
        {
            var catalog = Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeCurvedDirectionalEffects");
            Assert.IsNotNull(catalog); Assert.AreEqual(4, catalog.PatternCount); Assert.AreEqual(10, catalog.MagicCount);
            foreach (int id in new[] { 2601,8101,8102,8103,10801,10802,10803,10804,10805,17101 })
            {
                Assert.IsTrue(catalog.TryGet(id, out var profile)); Assert.IsTrue(profile.DirectionalTravel); Assert.IsFalse(profile.LinearTravel);
                Assert.AreEqual(8, profile.ProjectileCount);
                for (int i = 0; i < 10 + profile.ImpactCount; i++) Assert.AreEqual(i, profile.CounterForAge(i * GameConstants.BattleMagicEffectCounterSeconds));
                Assert.AreEqual(-1, profile.CounterForAge((10 + profile.ImpactCount) * GameConstants.BattleMagicEffectCounterSeconds));
            }
            var lift = Vector3.up * 50f / 32;
            var start = Vector3.zero; var target = Vector3.right * 8;
            var rising = NativeFireballEffects.TravelPosition(start, target, .1f, lift);
            var falling = NativeFireballEffects.TravelPosition(start, target, .9f, lift)
                - NativeFireballEffects.TravelPosition(start, target, .8f, lift);
            Assert.AreEqual(1, NativeFireballEffects.DirectionFrame(rising));
            Assert.AreEqual(3, NativeFireballEffects.DirectionFrame(falling));
        }

        [Test]
        public void LinearProjectileUsesOriginalEightDirectionsAndCounterSteps()
        {
            var catalog = Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeLinearProjectileEffects");
            Assert.IsNotNull(catalog); Assert.AreEqual(1, catalog.PatternCount); Assert.AreEqual(1, catalog.MagicCount);
            Assert.IsTrue(catalog.TryGet(10806, out var profile)); Assert.IsTrue(profile.LinearTravel);
            Assert.AreEqual(8, profile.ProjectileCount); Assert.AreEqual(4, profile.ImpactCount);
            int[] expected = {0,2,4,6,8,10,11,12,13,-1};
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], profile.CounterForAge(i * GameConstants.BattleMagicEffectCounterSeconds));
            Assert.AreEqual(-1, profile.CounterForAge(float.NaN)); Assert.AreEqual(-1, profile.CounterForAge(float.PositiveInfinity));
            var directions = new[] { Vector3.up, new Vector3(1,1), Vector3.right, new Vector3(1,-1), Vector3.down, new Vector3(-1,-1), Vector3.left, new Vector3(-1,1) };
            for (int i = 0; i < 8; i++) Assert.AreEqual(i, NativeFireballEffects.DirectionFrame(directions[i]));
            Assert.AreEqual(-1, NativeFireballEffects.DirectionFrame(Vector3.zero));
            Assert.AreEqual(new Vector3(.1875f,-.1875f,0), NativeFireballEffects.LinearPosition(Vector3.zero, new Vector3(1,-1), 2, Vector2.one));
        }

        [Test]
        public void LavaDrawsImmediatelyButSkipsTransitionCounter()
        {
            var catalog = Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeLavaEffects");
            Assert.IsNotNull(catalog); Assert.AreEqual(1, catalog.PatternCount); Assert.AreEqual(1, catalog.MagicCount);
            Assert.IsTrue(catalog.TryGet(2801, out var lava));
            Assert.AreEqual(3, lava.ProjectileCount); Assert.AreEqual(5, lava.ImpactCount);
            for (int i = 0; i < 15; i++) Assert.AreEqual(i != 10, lava.DrawsAtCounter(i));
            Assert.IsFalse(lava.DrawsAtCounter(-1)); Assert.IsFalse(lava.DrawsAtCounter(15));
            var fire = Resources.Load<NativeFireballEffects>("Effects/NativeFireballEffects");
            Assert.IsFalse(fire.DrawsAtCounter(0)); Assert.IsTrue(fire.DrawsAtCounter(10));
        }

        [Test]
        public void HeldShrinkTargetAnimatesThenHoldsOriginalModuloFrame()
        {
            var catalog = Resources.Load<NativeTargetEffects>("Effects/NativeHeldShrinkTargetEffects");
            Assert.IsNotNull(catalog); Assert.AreEqual(3, catalog.PatternCount); Assert.AreEqual(17, catalog.MagicCount);
            foreach (int id in new[] { 1501, 3601, 10407 })
            {
                Assert.IsTrue(catalog.TryGet(id, out var pattern)); Assert.AreEqual(10, pattern.LifetimeCounters);
                for (int i = 0; i < 5; i++)
                {
                    Assert.AreEqual(new Vector2(112f / pattern.Frame(i).rect.width, 112f / pattern.Frame(i).rect.height),
                        pattern.Scale(i, pattern.Frame(i).rect.width, pattern.Frame(i).rect.height));
                    Assert.AreSame(pattern.Frame(4), pattern.Frame(i + 5));
                    Assert.AreEqual(new Vector2((112 - i * 16f) / 32, (112 - i * 16f) / 32), pattern.Scale(i + 5, 32, 32));
                }
                Assert.IsNull(pattern.Frame(10)); Assert.IsNull(pattern.Frame(-1));
            }
            Assert.IsTrue(catalog.TryGet(10407, out var skull)); Assert.AreEqual(4, skull.FrameCount);
            Assert.AreSame(skull.Frame(0), skull.Frame(4)); Assert.AreSame(skull.Frame(0), skull.Frame(9));
            Assert.AreEqual(new Vector2(-5, 13), skull.Offset(43, 59));
            Assert.IsFalse(skull.CenterOnUnit);
        }

        [Test]
        public void SlowTargetUsesOriginalTwoCyclesAndMinimumCounterLimit()
        {
            var sprites = BattleAttackEffectFrames.Load();
            var one = new NativeTargetEffects.Pattern(50, new[] { 3801 }, new[] { sprites[0] }, repeatTwice: true);
            Assert.AreEqual(8, one.LifetimeCounters);
            var two = new NativeTargetEffects.Pattern(50, new[] { 3801 }, new[] { sprites[0], sprites[1] }, repeatTwice: true);
            Assert.AreEqual(4, two.LifetimeCounters); Assert.AreSame(sprites[1], two.Frame(3)); Assert.IsNull(two.Frame(4));
            var slow = Resources.Load<NativeTargetEffects>("Effects/NativeSlowTargetEffects");
            Assert.IsNotNull(slow); Assert.AreEqual(1, slow.PatternCount); Assert.AreEqual(10, slow.MagicCount);
            Assert.IsTrue(slow.TryGet(3801, out var pattern));
            Assert.AreEqual(50, pattern.Id); Assert.AreEqual(5, pattern.FrameCount); Assert.AreEqual(10, pattern.LifetimeCounters);
            Assert.IsFalse(pattern.CenterOnUnit);
            for (int i = 0; i < 5; i++) Assert.AreSame(pattern.Frame(i), pattern.Frame(i + 5));
            Assert.IsNull(pattern.Frame(10)); Assert.IsNull(pattern.Frame(-1));
        }

        [Test]
        public void ShrinkingEffectCentersOnSpriteWhileGroundEffectsKeepFootAnchor()
        {
            var texture = new Texture2D(32, 32);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .1f), 32);
            var go = new GameObject("Effect anchor regression");
            try
            {
                var body = go.AddComponent<SpriteRenderer>(); body.sprite = sprite;
                go.transform.position = new Vector3(4, 6, 0);
                var icon = new NativeTargetEffects.Pattern(43, new[] { 10901 }, new[] { sprite }, true);
                var groundEffect = new NativeTargetEffects.Pattern(41, new[] { 11201 }, new[] { sprite });
                Assert.That(Vector3.Distance(new Vector3(4, 6.4f, 0), BattleMapPresentation.GetNativeTargetEffectAnchor(body, icon)), Is.LessThan(.0001f));
                go.transform.localScale = new Vector3(2, 3, 1); body.flipY = true;
                Assert.That(Vector3.Distance(new Vector3(4, 4.8f, 0), BattleMapPresentation.GetNativeTargetEffectAnchor(body, icon)), Is.LessThan(.0001f));
                Assert.AreEqual(go.transform.position, BattleMapPresentation.GetNativeTargetEffectAnchor(body, groundEffect));
                Assert.AreEqual(Vector3.zero, BattleMapPresentation.GetNativeTargetEffectAnchor(null, icon));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void NativeShrinkingTargetUsesFiveFixedDestinationRectangles()
        {
            var sprites = BattleAttackEffectFrames.Load();
            var pattern = new NativeTargetEffects.Pattern(43, new[] { 10901 }, new[] { sprites[0] }, true);
            Assert.AreEqual(5, pattern.LifetimeCounters);
            for (int counter = 0; counter < 5; counter++)
            {
                Assert.AreSame(sprites[0], pattern.Frame(counter));
                Assert.AreEqual(new Vector2((112 - counter * 16) / 32f, (112 - counter * 16) / 32f), pattern.Scale(counter, 32, 32));
                Assert.AreEqual(Vector2.zero, pattern.Offset(32, 32));
            }
            Assert.IsNull(pattern.Frame(5)); Assert.IsNull(pattern.Frame(-1));
            Assert.AreEqual(new Vector2(-2, 3), pattern.Offset(36, 39));
            Assert.AreEqual(new Vector2(112f / 36, 112f / 39), pattern.Scale(0, 36, 39));
        }

        [Test]
        public void NativeTargetCounterUsesOriginalRepeatLimitAndSignedCentering()
        {
            var sprites = BattleAttackEffectFrames.Load();
            var pattern = new NativeTargetEffects.Pattern(60, new[] { 13101 }, new[] { sprites[0], sprites[1] });
            Assert.AreEqual(8, pattern.LifetimeCounters);
            Assert.AreSame(sprites[0], pattern.Frame(0)); Assert.AreSame(sprites[1], pattern.Frame(1));
            Assert.AreSame(sprites[0], pattern.Frame(2)); Assert.AreSame(sprites[1], pattern.Frame(7));
            Assert.IsNull(pattern.Frame(8)); Assert.IsNull(pattern.Frame(-1));
            var three = new NativeTargetEffects.Pattern(17, new[] { 1701 }, new[] { sprites[0], sprites[1], sprites[2] });
            Assert.AreEqual(3, three.LifetimeCounters); Assert.IsNull(three.Frame(3));
            Assert.AreEqual(-1, NativeTargetEffects.CounterAt(float.NaN));
            Assert.AreEqual(-1, NativeTargetEffects.CounterAt(float.PositiveInfinity));
            Assert.AreEqual(2, NativeTargetEffects.CounterAt(GameConstants.BattleMagicEffectCounterSeconds * 2));
            Assert.AreEqual(new Vector2(-.5f, -.5f), NativeTargetEffects.CenterAdjustment(31, 33));
            Assert.AreEqual(Vector2.zero, NativeTargetEffects.CenterAdjustment(96, 64));
        }

        [Test]
        public void NativeAttackFrameOrderRepeatsAndExpires()
        {
            var tick = GameConstants.BattleAttackCounterSeconds;
            Assert.That(BattleAttackEffectFrames.FrameAt(0), Is.EqualTo(0));
            Assert.That(BattleAttackEffectFrames.FrameAt(tick), Is.EqualTo(0));
            Assert.That(BattleAttackEffectFrames.FrameAt(tick * 2), Is.EqualTo(1));
            Assert.That(BattleAttackEffectFrames.FrameAt(tick * 6), Is.EqualTo(3));
            Assert.That(BattleAttackEffectFrames.FrameAt(tick * 8), Is.EqualTo(0));
            Assert.That(BattleAttackEffectFrames.FrameAt(tick * 50), Is.EqualTo(-1));
            Assert.That(BattleAttackEffectFrames.FrameAt(float.NaN), Is.EqualTo(-1));
            var frames = BattleAttackEffectFrames.Load();
            Assert.That(frames.Length, Is.EqualTo(4));
            Assert.That(System.Array.Exists(frames, f => f == null), Is.False);
            for (var i = 0; i < 4; i++) Assert.That(frames[i].rect.size, Is.EqualTo(new Vector2(32, 32)));
        }
        [Test]
        public void PlayerUsesNativeBaseColumnsAndCanRestoreSavedVitalsAfterwards()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Battle"); SceneManager.MoveGameObjectToScene(root, scene);
                var battle = root.AddComponent<BattleSceneRoot>();
                var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                Assert.That(jobs.TryGet(100, out var entry), Is.True);
                Assert.That(battle.ConfigurePlayerJobBaseStats(), Is.False);
                Assert.That(battle.ConfigureTraversalCatalogs(new[] { jobs }), Is.True);
                Assert.That(battle.ConfigurePlayerJob(100), Is.True);
                Assert.That(battle.ConfigurePlayerJobBaseStats(), Is.True);
                Assert.That(battle.TryGetUnit(100, out var unit), Is.True);
                Assert.That(unit.MaxHp, Is.EqualTo(entry.GetNumericValue(2)));
                Assert.That(unit.MaxMp, Is.EqualTo(entry.GetNumericValue(4)));
                Assert.That(unit.Attack, Is.EqualTo(entry.GetNumericValue(6)));
                Assert.That(unit.Defense, Is.EqualTo(entry.GetNumericValue(8)));
                Assert.That(battle.TryRestorePlayerVitals(12, 3), Is.True);
                Assert.That((battle.PlayerHp, battle.PlayerMp), Is.EqualTo((12, 3)));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void ActualAttackShowsDamageAndMissWithoutTreatingHpSyncAsAnAttack()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var floor = ScriptableObject.CreateInstance<Tile>();
            BattleMapPresentation view = null;
            try
            {
                GameObject Create(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
                var grid = Create("Grid").AddComponent<Grid>();
                var ground = Create("Ground").AddComponent<Tilemap>(); ground.transform.SetParent(grid.transform, false);
                var terrain = Create("TerrainAttributes").AddComponent<Tilemap>(); terrain.transform.SetParent(grid.transform, false);
                for (var x = 0; x < 4; x++) for (var y = 0; y < 4; y++) ground.SetTile(new Vector3Int(x, y), floor);
                var camera = Create("Camera").AddComponent<Camera>(); camera.orthographic = true;
                var ally = Create("Ally").AddComponent<SpriteRenderer>(); ally.transform.position = new Vector3(1.5f, 2.5f);
                var enemy = Create("Enemy").AddComponent<SpriteRenderer>(); enemy.transform.position = new Vector3(2.5f, 2.5f);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Down_1.png");
                ally.sprite = sprite; enemy.sprite = sprite;
                var root = Create("Battle"); root.AddComponent<BattleRequestSender>();
                Assert.That(root.AddComponent<BattlePacketReceiver>().Bind(), Is.True);
                view = root.AddComponent<BattleMapPresentation>();
                var battle = root.GetComponent<BattleSceneRoot>();
                var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                Assert.That(battle.ConfigureTraversalCatalogs(new[] { jobs }), Is.True);
                Assert.That(battle.ConfigurePlayerJob(100), Is.True);
                Assert.That(view.TryBind(ground, terrain, camera, new[] { new BattleMapVisual(100, ally), new BattleMapVisual(800, enemy) }), Is.True);
                battle.SetTemporaryLocalRules(true);
                Assert.That(battle.TrySelectUnit(100), Is.True); battle.AdvanceActionPoints(4);
                battle.ConfigurePhysicalAccuracy(100, 100, 0); battle.ConfigurePhysicalAccuracy(800, 100, 0);
                Assert.That(view.TryChooseBasicCommand(BattleMapCommand.Attack), Is.True);
                Assert.That(view.TryHandleWorldClick(enemy.transform.position), Is.True);
                Assert.That(view.CombatFeedbackCount, Is.EqualTo(1));
                Assert.That(view.ActiveAttackEffectCount, Is.EqualTo(2));
                var physicalHit = root.transform.Find("Native Attack Fx2 800");
                Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(enemy), physicalHit.position), Is.LessThan(.0001f));
                view.AdvanceAttackEffects(0);
                Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(enemy), physicalHit.position), Is.LessThan(.0001f));
                Assert.That(view.GetCombatFeedbackText(0), Is.EqualTo("-21"));
                battle.AdvanceActionPoints(7); battle.ConfigurePhysicalAccuracy(100, 0, 0);
                Assert.That(view.TryChooseBasicCommand(BattleMapCommand.Attack), Is.True);
                Assert.That(view.TryHandleWorldClick(enemy.transform.position), Is.True);
                Assert.That(view.GetCombatFeedbackText(1), Is.EqualTo("MISS"));
                view.AdvanceAttackEffects(GameConstants.BattleAttackCounterSeconds * 50);
                Assert.That(view.ActiveAttackEffectCount, Is.Zero);
                battle.ApplyHp(800, 54); Assert.That(view.CombatFeedbackCount, Is.EqualTo(2));
                Assert.That(battle.TryRestorePlayerVitals(60, 20), Is.True);
                Assert.That(view.CombatFeedbackCount, Is.EqualTo(2));
                Assert.That(battle.ApplyMp(100, 15), Is.True);
                Assert.That(view.GetCombatFeedbackText(2), Is.EqualTo("-5"));
                Assert.That(view.GetCombatFeedbackColorRow(2), Is.EqualTo(3));
                Assert.That(battle.ApplyHp(100, 84), Is.True);
                Assert.That(view.GetCombatFeedbackText(3), Is.EqualTo("+24"));
                Assert.That(view.GetCombatFeedbackColorRow(3), Is.EqualTo(1));
                Assert.That(view.GetCombatFeedbackColorRow(0), Is.Zero);
                battle.AdvanceActionPoints(7);
                Assert.That(battle.TryStartMagicRequest(101, 100, new short[] { 800 }, out _), Is.True);
                Assert.That(view.ActiveMagicEffectCount, Is.EqualTo(1));
                var afterMagic = (battle.PlayerHp, battle.EnemyHp, battle.PlayerMp, battle.PlayerAp);
                var receiver = root.GetComponent<BattlePacketReceiver>();
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(101, 100, 0, 0, 999, new short[] { 800 })), Is.True);
                Assert.That((battle.PlayerHp, battle.EnemyHp, battle.PlayerMp, battle.PlayerAp), Is.EqualTo(afterMagic));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 5);
                Assert.That(view.ActiveMagicEffectCount, Is.EqualTo(1));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 5);
                Assert.That(view.ActiveMagicEffectCount, Is.EqualTo(1));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 5);
                Assert.That(view.ActiveMagicEffectCount, Is.Zero);
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(301, 100, 0, 0, 0, new short[] { 800 })), Is.True);
                Assert.That(view.ActiveMagicEffectCount, Is.EqualTo(1));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 15);
                Assert.That(view.ActiveMagicEffectCount, Is.EqualTo(1));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                Assert.That(view.ActiveMagicEffectCount, Is.Zero);
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(501, 100, 0, 0, 999, new short[] { 800 })), Is.True);
                view.AdvanceMagicEffects(0);
                var thunderTarget = root.transform.Find("Native Fireball 800").GetComponent<SpriteRenderer>();
                var thunderCaster = root.transform.Find("Native Thunder Caster 100").GetComponent<SpriteRenderer>();
                Assert.That(thunderTarget.enabled, Is.True); Assert.That(thunderCaster.enabled, Is.True);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 10);
                Assert.That(thunderTarget.enabled, Is.True); Assert.That(thunderCaster.enabled, Is.False);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                Assert.That(root.transform.Find("Native Thunder Impact 800").GetComponent<SpriteRenderer>().enabled, Is.True);
                Assert.AreEqual(enemy.transform.position, root.transform.Find("Native Thunder Impact 800").position);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 1000);
                Assert.That(view.ActiveMagicEffectCount, Is.Zero);
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(701, 100, 0, 0, 0, new short[] { 800 })), Is.True);
                Assert.That(view.ActiveMagicEffectCount, Is.EqualTo(1));
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(801, 100, 1, 1, 999, new short[] { 800, 800 })), Is.True);
                view.AdvanceMagicEffects(0);
                Assert.That(view.ActiveMagicEffectCount, Is.EqualTo(1));
                int swirls = 0;
                foreach (Transform child in root.transform) if (child.name.StartsWith("Native Tornado Area"))
                { swirls++; Assert.That(child.GetComponent<SpriteRenderer>().enabled, Is.True); }
                Assert.That(swirls, Is.EqualTo(3));
                Assert.That(root.transform.Find("Native Tornado Target 800").GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("Tornado_4"));
                Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(enemy), root.transform.Find("Native Tornado Target 800").position), Is.LessThan(.0001f));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 9);
                Assert.That(view.ActiveMagicEffectCount, Is.EqualTo(1));
                foreach (Transform child in root.transform) if (child.name.StartsWith("Native Tornado Area"))
                    Assert.That(child.GetComponent<SpriteRenderer>().enabled, Is.False);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                Assert.That(view.ActiveMagicEffectCount, Is.Zero);
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(801, 100, 1, 1, 0, new short[] { 800 })), Is.True);
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(32000, 100, 0, 0, 0, new short[] { 800 })), Is.True);
                Assert.That(view.ActiveMagicEffectCount, Is.Zero);
                Assert.That((battle.PlayerHp, battle.EnemyHp, battle.PlayerMp, battle.PlayerAp), Is.EqualTo(afterMagic));
                view.AdvanceCombatFeedback(GameConstants.BattleFeedbackSeconds);
                var magicCatalog = AssetDatabase.LoadAssetAtPath<DarkSaver.Magic.MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                var nativeTargets = Resources.Load<NativeTargetEffects>("Effects/NativeTargetEffects");
                var expandingEffects=Resources.Load<NativeExpandingCasterEffects>("Effects/NativeExpandingCasterEffects");Assert.IsNotNull(expandingEffects);
                var doubleBeams=Resources.Load<NativeThrustBeamEffects>("Effects/NativeDoubleThrustBeamEffects");Assert.IsNotNull(doubleBeams);
                var plasmaBeams=Resources.Load<NativeThrustBeamEffects>("Effects/NativePlasmaBeamEffects");Assert.IsNotNull(plasmaBeams);
                enemy.gameObject.layer = 23;
                int specialRecords = 0, mappedRecords = 0;
                var unmappedIds=new System.Collections.Generic.List<int>();
                for (int i = 0; i < magicCatalog.Count; i++)
                {
                    var entry = magicCatalog.GetAt(i);
                    if (!BattleMapPresentation.IsNativeSpecialMenuId(entry.Id)) continue;
                    specialRecords++;
                    Assert.IsTrue(view.TryGetSpecialVisualMapping(magicCatalog, entry.Id, out var patternId, out var mapped));
                    Assert.AreEqual(entry.PatternId, patternId);
                    if (!mapped) {unmappedIds.Add(entry.Id);continue;}
                    mappedRecords++;
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification((short)entry.Id, 100, 0, 0, 0, new short[] { 800 })));
                    Assert.AreEqual(plasmaBeams.TryGetLifetime(entry.Id,out _) || doubleBeams.TryGetLifetime(entry.Id,out _) || expandingEffects.TryGet(entry.Id,out _) || entry.Id>=10601 && entry.Id<=10606 || entry.Id>=10701 && entry.Id<=10706 || entry.Id>=10101 && entry.Id<=10106?2:1, view.ActiveMagicEffectCount, entry.Name);
                    if (nativeTargets.TryGet(entry.Id, out _))
                        Assert.AreEqual(23, root.transform.Find("Native Target Effect 800").gameObject.layer);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 2);
                    Assert.AreEqual(plasmaBeams.TryGetLifetime(entry.Id,out _) || doubleBeams.TryGetLifetime(entry.Id,out _) || expandingEffects.TryGet(entry.Id,out _) || entry.Id>=10601 && entry.Id<=10606 || entry.Id>=10701 && entry.Id<=10706 || entry.Id>=10101 && entry.Id<=10106?2:1, view.ActiveMagicEffectCount, entry.Name);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 1000);
                    Assert.AreEqual(0, view.ActiveMagicEffectCount, entry.Name);
                }
                Assert.AreEqual(86, specialRecords); Assert.AreEqual(85, mappedRecords);
                CollectionAssert.AreEquivalent(new[]{30901},unmappedIds,"Curism remains explicitly unconfirmed; do not manufacture an effect");
                for (short groundId = 10601; groundId <= 10606; groundId++)
                {
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(groundId, 100, 0, 0, 0, new short[] { 800,800 })));
                    view.AdvanceMagicEffects(0); Assert.AreEqual(2, view.ActiveMagicEffectCount);
                    var burst=root.transform.Find("Native Ground Burst Caster 100");Assert.IsNotNull(burst);
                    var cells=burst.GetComponentsInChildren<SpriteRenderer>();Assert.GreaterOrEqual(cells.Length,7);
                    var snapshots=new (Sprite,bool)[cells.Length];
                    for(int j=0;j<cells.Length;j++)
                    {
                        snapshots[j]=(cells[j].sprite,cells[j].enabled);Assert.AreEqual(ally.gameObject.layer,cells[j].gameObject.layer);
                        if(cells[j].enabled)Assert.That(cells[j].transform.position.y%1,Is.EqualTo(.5f).Within(.0001f));
                    }
                    view.AdvanceMagicEffects(0);
                    for(int j=0;j<cells.Length;j++)Assert.AreEqual(snapshots[j],(cells[j].sprite,cells[j].enabled));
                    var body = root.transform.Find("Native Target Effect 800").GetComponent<SpriteRenderer>();
                    Assert.IsTrue(body.enabled); var first = body.sprite;
                    Assert.That(Vector3.Distance(enemy.transform.position, body.transform.position), Is.LessThan(.0001f));
                    view.AdvanceMagicEffects(0); Assert.AreSame(first, body.sprite); Assert.AreEqual(23, body.gameObject.layer);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 1000); Assert.AreEqual(0, view.ActiveMagicEffectCount);
                    Assert.IsFalse(battle.TryGetTemporaryMagicMpCost(groundId, out _));
                }
                var delayedCatalog = Resources.Load<NativeTargetEffects>("Effects/NativeDelayedTargetEffects");
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(10601,100,0,0,0,new short[0])));
                view.AdvanceMagicEffects(0);Assert.AreEqual(1,view.ActiveMagicEffectCount);
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(32000,100,0,0,0,new short[0])));
                Assert.AreEqual(0,view.ActiveMagicEffectCount);
                foreach(var part in root.transform.Find("Native Ground Burst Caster 100").GetComponentsInChildren<SpriteRenderer>())Assert.IsFalse(part.enabled);
                int delayedCount = 0;
                for (int i = 0; i < magicCatalog.Count; i++)
                {
                    var entry = magicCatalog.GetAt(i); if (!delayedCatalog.TryGet(entry.Id, out var pattern)) continue;
                    delayedCount++;
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification((short)entry.Id, 100, 0, 0, 0, new short[] { 800,800 })));
                    view.AdvanceMagicEffects(0); Assert.AreEqual(plasmaBeams.TryGetLifetime(entry.Id,out _) || doubleBeams.TryGetLifetime(entry.Id,out _) || expandingEffects.TryGet(entry.Id,out _) || entry.Id==4001 || entry.Id>=10701 && entry.Id<=10706 || entry.Id>=10101 && entry.Id<=10106?2:1, view.ActiveMagicEffectCount);
                    var body = root.transform.Find("Native Target Effect 800").GetComponent<SpriteRenderer>();
                    Assert.IsFalse(body.enabled);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 10); Assert.IsFalse(body.enabled);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                    Assert.IsTrue(body.enabled); Assert.AreSame(pattern.Frame(11), body.sprite); Assert.AreEqual(23, body.gameObject.layer);
                    Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(enemy), body.transform.position), Is.LessThan(.0001f));
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 13); Assert.AreSame(pattern.Frame(24), body.sprite);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                    Assert.AreEqual(doubleBeams.TryGetLifetime(entry.Id,out int doubleLife) && doubleLife>25 || expandingEffects.TryGet(entry.Id,out var expandingEntry) && expandingEntry.Lifetime>25 || entry.Id==10105 || entry.Id==10106?1:0,view.ActiveMagicEffectCount);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*1000); Assert.AreEqual(0, view.ActiveMagicEffectCount);
                }
                Assert.AreEqual(40, delayedCount);
                for(short id=10301;id<=10306;id++)for(short direction=-1;direction>=-4;direction--)
                {
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(id,100,direction,0,0,new short[]{800,800})));
                    view.AdvanceMagicEffects(0);Assert.AreEqual(2,view.ActiveMagicEffectCount);
                    var beam=root.transform.Find("Native Thrust Beam 100");int enabled=0;
                    foreach(var part in beam.GetComponentsInChildren<SpriteRenderer>()){if(part.enabled)enabled++;Assert.AreEqual(ally.gameObject.layer,part.gameObject.layer);}
                    Assert.AreEqual(direction==-1?0:6,enabled);
                    if(direction!=-1)
                    {
                        var first=beam.Find("Body").GetComponent<SpriteRenderer>();var second=beam.Find("Second Body").GetComponent<SpriteRenderer>();
                        Assert.That(Vector3.Distance(first.transform.position,second.transform.position),Is.EqualTo(2).Within(.0001f));
                    }
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*1000);Assert.AreEqual(0,view.ActiveMagicEffectCount);
                }
                foreach(short id in new short[]{6601,6602,6603,10401,10402,10403,10404,10405,10406})
                {
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(id,100,0,0,0,new short[]{800,800})));
                    view.AdvanceMagicEffects(0);Assert.AreEqual(2,view.ActiveMagicEffectCount);
                    var body=root.transform.Find("Native Expanding Caster 100").GetComponent<SpriteRenderer>();Assert.IsTrue(body.enabled);
                    Assert.AreEqual(ally.gameObject.layer,body.gameObject.layer);Assert.AreEqual(ally.transform.position,body.transform.position);
                    expandingEffects.TryGet(id,out var entry);Assert.AreSame(entry.Frame(0),body.sprite);
                    var size=entry.PixelSize(0);Assert.AreEqual(new Vector3(size.x/body.sprite.rect.width,size.y/body.sprite.rect.height,1),body.transform.localScale);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*6);Assert.AreSame(entry.Frame(6),body.sprite);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*1000);Assert.AreEqual(0,view.ActiveMagicEffectCount);Assert.IsFalse(body.enabled);
                }
                for(short direction=-1;direction>=-4;direction--)
                {
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(10101,100,direction,0,0,new short[]{800,800})));
                    view.AdvanceMagicEffects(0);Assert.AreEqual(2,view.ActiveMagicEffectCount);
                    var beam=root.transform.Find("Native Thrust Beam 100");Assert.IsNotNull(beam);
                    var body=beam.Find("Body").GetComponent<SpriteRenderer>();Assert.IsTrue(body.enabled);
                    Assert.AreEqual(SpriteDrawMode.Sliced,body.drawMode);Assert.AreEqual(ally.gameObject.layer,body.gameObject.layer);
                    Assert.AreEqual(direction<=-3?new Vector2(22,34)/32:new Vector2(34,22)/32,body.size);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*5);
                    Assert.AreEqual(direction<=-3?new Vector2(22,68)/32:new Vector2(68,22)/32,body.size);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*5);
                    Assert.IsFalse(body.enabled);Assert.AreEqual(1,view.ActiveMagicEffectCount);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*15);Assert.AreEqual(0,view.ActiveMagicEffectCount);
                }
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(10701,100,3,1,0,new short[]{800,800})));
                view.AdvanceMagicEffects(0);Assert.AreEqual(2,view.ActiveMagicEffectCount);
                var salamander=root.transform.Find("Native Salamander Caster 100");Assert.IsNotNull(salamander);
                var head=salamander.Find("Head").GetComponent<SpriteRenderer>();
                Assert.IsTrue(head.enabled);Assert.AreEqual("Salamander_0",head.sprite.name);
                Assert.That(Vector3.Distance(ally.transform.position+new Vector3(19.5f/32,0,0),head.transform.position),Is.LessThan(.0001f));
                int trails=0;foreach(var body in salamander.GetComponentsInChildren<SpriteRenderer>())
                {if(body.enabled)trails++;Assert.AreEqual(ally.gameObject.layer,body.gameObject.layer);}
                Assert.AreEqual(4,trails); // Three flames and one head; duplicate targets do not duplicate caster.
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*6);
                Assert.IsFalse(head.enabled);Assert.AreEqual(1,view.ActiveMagicEffectCount);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*19);Assert.AreEqual(0,view.ActiveMagicEffectCount);
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(10706,100,0,1,0,new short[0])));
                view.AdvanceMagicEffects(0);Assert.IsTrue(head.enabled);Assert.AreEqual(1,view.ActiveMagicEffectCount);
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(32000,100,0,0,0,new short[0])));
                Assert.IsFalse(head.enabled);Assert.AreEqual(0,view.ActiveMagicEffectCount);
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(4001,100,0,0,0,new short[]{100,100})));
                view.AdvanceMagicEffects(0);Assert.AreEqual(2,view.ActiveMagicEffectCount);
                var circleRoot=root.transform.Find("Native Level Up Circle 100");Assert.IsNotNull(circleRoot);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*6);
                int visiblePieces=0;
                foreach(var body in circleRoot.GetComponentsInChildren<SpriteRenderer>())
                {if(body.enabled)visiblePieces++;Assert.AreEqual(ally.gameObject.layer,body.gameObject.layer);Assert.AreEqual(ally.sortingOrder-1,body.sortingOrder);}
                Assert.AreEqual(6,visiblePieces);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*6);
                visiblePieces=0;foreach(var body in circleRoot.GetComponentsInChildren<SpriteRenderer>())if(body.enabled)visiblePieces++;
                Assert.AreEqual(12,visiblePieces);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*10);
                foreach(var body in circleRoot.GetComponentsInChildren<SpriteRenderer>())Assert.IsFalse(body.enabled);
                Assert.AreEqual(2,view.ActiveMagicEffectCount);Assert.IsTrue(view.IsNativeLevelUpFlashVisible); // Last native white fill/redraw occurs at22 even though pieces are hidden.
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);Assert.IsFalse(view.IsNativeLevelUpFlashVisible);Assert.AreEqual(1,view.ActiveMagicEffectCount);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*2);Assert.AreEqual(0,view.ActiveMagicEffectCount);
                var curvedCatalog = Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeCurvedDirectionalEffects");
                foreach (short curvedId in new short[] { 2601,8101,8102,8103,10801,10802,10803,10804,10805,17101 })
                {
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(curvedId, 100, 0, 0, 0, new short[] { 800,800 })));
                    Assert.IsTrue(curvedCatalog.TryGet(curvedId, out var profile));
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                    var body = root.transform.Find("Native Fireball 800").GetComponent<SpriteRenderer>();
                    Assert.IsTrue(body.enabled); Assert.AreEqual(23, body.gameObject.layer);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 9);
                    Assert.AreSame(profile.Impact(0), body.sprite);
                    Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(enemy), body.transform.position), Is.LessThan(.0001f));
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * profile.ImpactCount);
                    Assert.AreEqual(0, view.ActiveMagicEffectCount);
                    Assert.IsFalse(battle.TryGetTemporaryMagicMpCost(curvedId, out _));
                }
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(10806, 100, 0, 0, 0, new short[] { 800, 800 })));
                view.AdvanceMagicEffects(0); Assert.AreEqual(1, view.ActiveMagicEffectCount);
                var linearBody = root.transform.Find("Native Fireball 800").GetComponent<SpriteRenderer>();
                Assert.IsFalse(linearBody.enabled);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                Assert.IsTrue(linearBody.enabled); Assert.AreEqual(23, linearBody.gameObject.layer);
                var linearCatalog = Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeLinearProjectileEffects");
                Assert.IsTrue(linearCatalog.TryGet(10806, out var linear));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 4);
                Assert.AreSame(linear.Impact(0), linearBody.sprite);
                Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(enemy), linearBody.transform.position), Is.LessThan(.0001f));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 4);
                Assert.AreEqual(0, view.ActiveMagicEffectCount);
                Assert.IsFalse(battle.TryGetTemporaryMagicMpCost(10806, out _));
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(2801, 100, 0, 0, 0, new short[] { 800, 800 })));
                view.AdvanceMagicEffects(0); Assert.AreEqual(1, view.ActiveMagicEffectCount);
                var lavaBody = root.transform.Find("Native Fireball 800").GetComponent<SpriteRenderer>();
                Assert.IsTrue(lavaBody.enabled); Assert.IsNotNull(lavaBody.sprite); Assert.AreEqual(23, lavaBody.gameObject.layer);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 10);
                Assert.IsFalse(lavaBody.enabled); Assert.AreEqual(1, view.ActiveMagicEffectCount);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                var lavaCatalog = Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeLavaEffects");
                Assert.IsTrue(lavaCatalog.TryGet(2801, out var lava));
                Assert.IsTrue(lavaBody.enabled); Assert.AreSame(lava.Impact(1), lavaBody.sprite);
                Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(enemy), lavaBody.transform.position), Is.LessThan(.0001f));
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 4);
                Assert.AreEqual(0, view.ActiveMagicEffectCount);
                Assert.IsFalse(battle.TryGetTemporaryMagicMpCost(2801, out _));
                foreach (short heldId in new short[] { 1501,1502,1503,1504,1505,1506,5501,5502,5503,5504,3601,3602,3603,3604,3605,3606,10407 })
                {
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(heldId, 100, 0, 0, 0, new short[] { 800, 800 })));
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 4);
                    var body = root.transform.Find("Native Target Effect 800").GetComponent<SpriteRenderer>();
                    var held = body.sprite; Assert.IsNotNull(held); Assert.AreEqual(23, body.gameObject.layer);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 5);
                    Assert.AreSame(held, body.sprite); Assert.AreEqual(1, view.ActiveMagicEffectCount);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                    Assert.AreEqual(0, view.ActiveMagicEffectCount);
                    Assert.IsFalse(battle.TryGetTemporaryMagicMpCost(heldId, out _));
                }
                for (short slowId = 3801; slowId <= 3810; slowId++)
                {
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(slowId, 100, 0, 0, 0, new short[] { 800, 800 })));
                    view.AdvanceMagicEffects(0); Assert.AreEqual(1, view.ActiveMagicEffectCount);
                    var slowBody = root.transform.Find("Native Target Effect 800").GetComponent<SpriteRenderer>();
                    var first = slowBody.sprite; Assert.AreEqual(23, slowBody.gameObject.layer);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 5);
                    Assert.AreSame(first, slowBody.sprite); Assert.AreEqual(1, view.ActiveMagicEffectCount);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 5);
                    Assert.AreEqual(0, view.ActiveMagicEffectCount);
                    Assert.IsFalse(battle.TryGetTemporaryMagicMpCost(slowId, out _));
                }
                // fxptn 16's target branch only clears its state and redraws the unit.
                // Its separate caster renderer is now verified; still never invent a target hit.
                foreach (short crossTrust in new short[] { 10201, 10202, 10203, 10204, 10205 })
                {
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(13101, 100, 0, 0, 0, new short[] { 800 })));
                    view.AdvanceMagicEffects(0); Assert.AreEqual(1, view.ActiveMagicEffectCount);
                    Assert.IsTrue(view.TryGetSpecialVisualMapping(magicCatalog, crossTrust, out var crossPattern, out var crossMapped));
                    Assert.AreEqual(42, crossPattern); Assert.IsTrue(crossMapped);
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(crossTrust, 100, 0, 0, 0, new short[] { 800 })));
                    view.AdvanceMagicEffects(0); Assert.AreEqual(1, view.ActiveMagicEffectCount);
                    Assert.IsFalse(root.transform.Find("Native Target Effect 800").GetComponent<SpriteRenderer>().enabled);
                    var crossRoot = root.transform.Find("Native Cross Trust Caster 100");
                    Assert.IsNotNull(crossRoot); int segments = 0;
                    foreach(var body in crossRoot.GetComponentsInChildren<SpriteRenderer>())
                    { if(body.enabled)segments++; Assert.AreEqual(ally.gameObject.layer,body.gameObject.layer); }
                    Assert.AreEqual(2,segments);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*3);
                    segments=0; foreach(var body in crossRoot.GetComponentsInChildren<SpriteRenderer>())if(body.enabled)segments++;
                    Assert.AreEqual(14,segments);
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*1000);
                    Assert.AreEqual(0,view.ActiveMagicEffectCount);
                    Assert.IsFalse(battle.TryGetTemporaryMagicMpCost(crossTrust, out _));
                }
                Assert.IsTrue(view.TryGetSpecialVisualMapping(magicCatalog, 13101, out _, out var recovered)); Assert.IsTrue(recovered);
                var metadata=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");Assert.IsNotNull(metadata);
                // 30901 is a menu/rule record with no recovered native visual metadata.
                Assert.IsTrue(magicCatalog.TryGet(30901,out _));
                Assert.IsFalse(metadata.TryGet(30901,out _));
                Assert.AreEqual(magicCatalog.Count-1,metadata.Count);
                for(int catalogIndex=0;catalogIndex<magicCatalog.Count;catalogIndex++)
                {
                    var rule=magicCatalog.GetAt(catalogIndex);
                    if(rule.Id!=30901)Assert.IsTrue(metadata.TryGet(rule.Id,out _),rule.Id.ToString());
                }
                int visualRecords=0,noVisualRecords=0;
                for(int i=0;i<metadata.Count;i++)
                {
                    var entry=metadata.GetAt(i);Assert.LessOrEqual(entry.Id,short.MaxValue);
                    Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification((short)entry.Id,100,-2,1,0,new short[]{800,800})));
                    view.AdvanceMagicEffects(0);
                    if(entry.Family==0 || entry.Family==99){noVisualRecords++;Assert.AreEqual(0,view.ActiveMagicEffectCount,entry.Id.ToString());}
                    else{visualRecords++;Assert.Greater(view.ActiveMagicEffectCount,0,entry.Id.ToString());}
                    if(entry.SharedCaster)
                    {
                        var charge=root.transform.Find("Native Thunder Caster 100").GetComponent<SpriteRenderer>();Assert.IsTrue(charge.enabled);
                        Assert.AreEqual(ally.gameObject.layer,charge.gameObject.layer);Assert.AreEqual(ally.transform.position,charge.transform.position);
                        view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*10);Assert.IsFalse(charge.enabled);
                    }
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*1000);Assert.AreEqual(0,view.ActiveMagicEffectCount,entry.Id.ToString());
                }
                Assert.AreEqual(metadata.Count,visualRecords+noVisualRecords);
                int beforeAudio=view.NativeMagicSoundStartCount;
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(1001,100,2,1,0,new short[]{800})));
                view.AdvanceMagicEffects(0);view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*4);
                int meteorPieces=0;
                foreach(var part in root.GetComponentsInChildren<SpriteRenderer>())if(part.name.StartsWith("Native Coordinate Area 4 ") && part.enabled)meteorPieces++;
                for(int retry=0;meteorPieces==0 && retry<4;retry++)
                {
                    // A valid native random sequence can remain at zero; observe a later counter without changing RNG.
                    view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                    foreach(var part in root.GetComponentsInChildren<SpriteRenderer>())if(part.name.StartsWith("Native Coordinate Area 4 ") && part.enabled)meteorPieces++;
                }
                Assert.Greater(meteorPieces,0,"Meteor coordinate pass must follow the updated target counter.");
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*1000);
                beforeAudio=view.NativeMagicSoundStartCount;
                Assert.IsTrue(receiver.TryApplyPacket(new MagicEffectNotification(10806,100,2,1,0,new short[]{800,800})));
                view.AdvanceMagicEffects(0);Assert.AreEqual(beforeAudio+1,view.NativeMagicSoundStartCount);Assert.AreEqual(18,view.LastNativeMagicSoundId);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*4);Assert.AreEqual(beforeAudio+1,view.NativeMagicSoundStartCount);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);Assert.AreEqual(beforeAudio+2,view.NativeMagicSoundStartCount);Assert.AreEqual(7,view.LastNativeMagicSoundId);
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*1000);
                Assert.IsFalse(view.TryGetSpecialVisualMapping(magicCatalog, 101, out _, out _));
                Assert.IsFalse(view.TryGetSpecialVisualMapping(magicCatalog, 19999, out _, out _));
                Assert.That((battle.PlayerHp, battle.EnemyHp, battle.PlayerMp, battle.PlayerAp), Is.EqualTo(afterMagic));
                Assert.That(view.CombatFeedbackCount, Is.Zero);
                view.SuspendPresentation(); Assert.That(view.CombatFeedbackCount, Is.Zero);
                Assert.That(view.ActiveAttackEffectCount, Is.Zero);
                view.ReleaseMagicEffectResources();
                view.ReleaseAttackEffectResources(); Assert.That(root.transform.childCount, Is.Zero);
            }
            finally
            {
                if (view != null) view.ReleaseMagicPreviewResources();
                EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(floor);
            }
        }
    }
}
