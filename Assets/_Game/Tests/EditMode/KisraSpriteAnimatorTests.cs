using System.Linq;
using System.Reflection;
using DarkSaver.Editor;
using DarkSaver.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class KisraSpriteAnimatorTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ThreeFramesPingPongAtMillisecondBoundaries(bool moving)
        {
            var interval = System.Math.Max(1, moving
                ? GameConstants.CharacterMoveFrameMilliseconds : GameConstants.CharacterIdleFrameMilliseconds);
            var expected = new[] { 0, 1, 2, 1, 0, 1, 2, 1, 0 };
            for (var step = 0; step < expected.Length; step++)
            {
                var time = (long)interval * step;
                Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds(time, 3, moving), Is.EqualTo(expected[step]));
                if (step > 0)
                    Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds(time - 1, 3, moving), Is.EqualTo(expected[step - 1]));
            }
            Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds((long)interval * 4000000001L, 3, moving), Is.EqualTo(1));
        }

        [Test]
        public void MissingSingleAndTwoFrameSequencesAreSafe()
        {
            Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds(0, 0, false), Is.EqualTo(-1));
            Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds(0, -1, false), Is.EqualTo(-1));
            Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds(long.MaxValue, 1, true), Is.Zero);
            Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds(-1, 3, true), Is.Zero);
            var interval = System.Math.Max(1, GameConstants.CharacterMoveFrameMilliseconds);
            Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds(interval, 2, true), Is.EqualTo(1));
            Assert.That(DirectionalSpriteAnimator.FrameAtMilliseconds(interval * 2L, 2, true), Is.Zero);
        }

        [Test]
        public void NativeFramesHaveExpectedImportSettings()
        {
            foreach (var direction in new[] { "Up", "Down", "Left", "Right" })
            foreach (var frame in Enumerable.Range(0, 3))
            {
                var path = KisraSpriteConverter.FramePath(direction, frame);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(64, 64)), path);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(32), path);
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
            }
        }

        [TestCase(0f, 0f, "Down")]
        [TestCase(0f, 1f, "Up")]
        [TestCase(0f, -1f, "Down")]
        [TestCase(-1f, 0f, "Right")]
        [TestCase(1f, 0f, "Left")]
        [TestCase(-1f, 1f, "Right")]
        [TestCase(-1f, -1f, "Right")]
        [TestCase(1f, 1f, "Left")]
        [TestCase(1f, -1f, "Left")]
        public void MotionUsesExpectedSourceRow(float x, float y, string sourceRow)
        {
            // Source names are intentionally swapped once by DirectionalSpriteAnimator.
            CheckMotion(new Vector2(x, y), sourceRow, false);
        }

        [Test]
        public void StoppingKeepsLastFacing()
        {
            CheckMotion(Vector2.left, "Right", true);
        }

        private static void CheckMotion(Vector2 motion, string sourceRow, bool stop)
        {
            // A preview scene isolates tests from open and potentially unsaved user scenes.
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Kisra Animator Test", typeof(SpriteRenderer));
                SceneManager.MoveGameObjectToScene(go, preview);
                var animator = go.AddComponent<DirectionalSpriteAnimator>();
                animator.Configure(Frames("Up"), Frames("Down"), Frames("Left"), Frames("Right"));
                Invoke(animator, "Awake");
                animator.SetMotion(motion);
                if (stop) animator.SetMotion(Vector2.zero);
                Invoke(animator, "Update");
                var shown=go.GetComponent<SpriteRenderer>().sprite;
                Assert.IsTrue(Frames(sourceRow).Any(frame=>frame.texture==shown.texture && frame.rect==shown.rect));
                Assert.That(shown.bounds.center.sqrMagnitude,Is.LessThan(.00001f));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static Sprite[] Frames(string direction) => Enumerable.Range(0, 3)
            .Select(frame => AssetDatabase.LoadAssetAtPath<Sprite>(KisraSpriteConverter.FramePath(direction, frame)))
            .ToArray();

        private static void Invoke(DirectionalSpriteAnimator animator, string method) =>
            typeof(DirectionalSpriteAnimator).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(animator, null);
    }
}
