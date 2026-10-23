using DarkSaver.Editor;
using DarkSaver.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class MercenaryArtworkTests
    {
        [Test]
        public void NativeFramesMatchAndEachCopyKeepsCorrectScreenFacing()
        {
            MercenarySpriteConverter.ConvertInitialSheets(); // Verifies existing pixels, no rewrite.
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (int sheet in new[] {200, 207})
                {
                    var source = new GameObject("Native template"); SceneManager.MoveGameObjectToScene(source, scene);
                    var template = source.AddComponent<DirectionalSpriteAnimator>(); MercenarySpriteConverter.ConfigureTemplate(sheet, template);
                    var target = new GameObject("Deployed copy"); SceneManager.MoveGameObjectToScene(target, scene);
                    var animator = target.AddComponent<DirectionalSpriteAnimator>();
                    Assert.That(template.TryCopyConfigurationTo(animator), Is.True);
                    Assert.That(animator.HasCompleteDirections, Is.True);
                    animator.TrySetFacing(Vector2.left); animator.PresentFrame(0);
                    Assert.That(target.GetComponent<SpriteRenderer>().sprite,
                        Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(MercenarySpriteConverter.FramePath(sheet, "Right", 0))));
                    animator.TrySetFacing(Vector2.down); animator.PresentFrame(0);
                    Assert.That(target.GetComponent<SpriteRenderer>().sprite,
                        Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(MercenarySpriteConverter.FramePath(sheet, "Down", 0))));
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [Test]
        public void InfoWindowUsesMercenaryJobSheetMappingAndOriginalMiddleCrop()
        {
            var jobs=AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>(JobCatalogConverter.MercenaryOutput);
            var icons=Resources.Load<DarkSaver.UI.MercenaryInfoIconCatalog>("UI/MercenaryInfoIcons/MercenaryInfoIcons");
            Assert.That(icons,Is.Not.Null);
            int expected=0;
            for(int i=0;i<jobs.Count;i++) {
                var job=jobs.GetAt(i);
                if(job.SpriteId!=200 && job.SpriteId!=207)continue;
                expected++;
                Assert.That(icons.TryGet(job.Id,out var icon),Is.True);
                Assert.That(icon,Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(MercenarySpriteConverter.FramePath((int)job.SpriteId,"Down",1))));
                Assert.That(icon.rect,Is.EqualTo(new Rect(0,0,32,32)));
            }
            Assert.That(icons.Count,Is.EqualTo(expected));Assert.That(expected,Is.GreaterThan(0));
            Assert.That(icons.TryGet(105,out _),Is.False);
            Assert.That(icons.TryGet(-1,out _),Is.False);
            Assert.That(icons.TryGet(9999,out _),Is.False);
            var root=new GameObject("Mercenary info icon lookup");
            try {
                var panel=root.AddComponent<DarkSaver.UI.TownServicesPanel>();
                Assert.That(panel.CharacterInfoMercenaryIcon(200),Is.Not.Null);
                Assert.That(panel.CharacterInfoMercenaryIcon(105),Is.Null);
                Assert.That(panel.CharacterInfoMercenaryIcon(9999),Is.Null);
            } finally {Object.DestroyImmediate(root);}
        }
        [Test]
        public void SavedCoreReferencesBothNativeMercenarySheets()
        {
            var dependencies = AssetDatabase.GetDependencies("Assets/_Game/Scenes/Runtime/GameCore.unity", true);
            foreach (int sheet in new[] {200, 207}) foreach (string direction in new[] {"Up", "Down", "Left", "Right"})
                for (int frame = 0; frame < 3; frame++)
                    Assert.That(dependencies, Does.Contain(MercenarySpriteConverter.FramePath(sheet, direction, frame)));
        }
    }
}
