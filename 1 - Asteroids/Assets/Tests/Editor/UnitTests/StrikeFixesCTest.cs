using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The HUD bars that only move when a step shows, the drop shadows' shared theme materials, and the built strike
    /// levels (every one of them valid, as the batch build now demands).
    /// </summary>
    public class StrikeFixesCTest
    {
        [Test]
        public void CreepingProgressMovesTheBarOnlyAStepAtATime()
        {
            // The progress of a scrolling mission grows by about 0.00013 a frame: the bar holds until a step is crossed.
            float fill = StrikeUI.StepFill(0f, 0.1f, 1f);
            int changes = 0;
            float progress = 0.1f;
            for (int frame = 0; frame < 600; frame++)
            {
                progress += 0.00013f;
                float next = StrikeUI.StepFill(fill, progress, 0.05f);
                if (next != fill)
                {
                    changes++;
                    Assert.That(Mathf.Abs(next / StrikeUI.FillStep - Mathf.Round(next / StrikeUI.FillStep)), Is.LessThan(0.001f),
                        "A settled bar sits on a step.");
                }
                fill = next;
            }
            Assert.That(changes, Is.InRange(35, 45), "600 frames of creeping progress cross about 40 steps, not 600.");
            Assert.That(fill, Is.EqualTo(progress).Within(StrikeUI.FillStep));
        }


        [Test]
        public void BarStillEasesAndReachesItsEnds()
        {
            Assert.That(StrikeUI.StepFill(0f, 1f, 0.05f), Is.EqualTo(0.05f).Within(1e-6f), "A jump eases at the blend rate.");
            float fill = 0f;
            for (int frame = 0; frame < 40; frame++)
            {
                fill = StrikeUI.StepFill(fill, 1f, 0.05f);
            }
            Assert.AreEqual(1f, fill, "A full bar is full.");
            Assert.AreEqual(0f, StrikeUI.StepFill(0.3f, -2f, 1f), "An empty bar is empty.");
            Assert.AreEqual(1f, StrikeUI.StepFill(0.3f, 5f, 1f));
        }


        [Test]
        public void ShadowsOfATheme_ShareOneTintedMaterial()
        {
            var source = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Hidden/InternalErrorShader"));
            source.SetColor("_BaseColor", Color.white);
            var dusk = new Color(0f, 0f, 0.1f, 0.45f);
            var noon = new Color(0f, 0f, 0f, 0.3f);
            Material first = DropShadow.TintedMaterial(source, dusk);
            Material second = DropShadow.TintedMaterial(source, dusk);
            Material other = DropShadow.TintedMaterial(source, noon);
            try
            {
                Assert.IsNotNull(first);
                Assert.AreSame(first, second, "Every shadow of a theme draws with one material (SRP Batcher friendly).");
                Assert.AreNotSame(first, other);
                Assert.AreNotSame(source, first, "The material asset itself is never changed.");
                if (first.HasProperty("_BaseColor"))
                {
                    Assert.AreEqual(dusk, first.GetColor("_BaseColor"));
                    Assert.AreEqual(noon, other.GetColor("_BaseColor"));
                    Assert.AreEqual(Color.white, source.GetColor("_BaseColor"));
                }
                Assert.IsNull(DropShadow.TintedMaterial(null, dusk));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(other);
                Object.DestroyImmediate(source);
            }
        }


        [Test]
        public void DestroyedTintedMaterial_IsMadeAgain()
        {
            var source = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Hidden/InternalErrorShader"));
            var color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            Material first = DropShadow.TintedMaterial(source, color);
            Object.DestroyImmediate(first);
            Material again = DropShadow.TintedMaterial(source, color);
            try
            {
                Assert.IsTrue(again != null, "A cached copy that was destroyed (a scene unload) is made again.");
            }
            finally
            {
                Object.DestroyImmediate(again);
                Object.DestroyImmediate(source);
            }
        }


        [Test]
        public void EveryBuiltStrikeLevelIsValid()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(StrikeLevel).Assembly);
            string root = package != null ? package.assetPath : "Assets";
            int found = 0;
            for (int i = 1; i <= 9; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<StrikeLevel>($"{root}/Config/Strike/Level{i}.asset");
                if (level == null)
                {
                    continue;
                }
                found++;
                Assert.IsTrue(level.IsLevelValid(out string message), $"Level{i} ({level.Title}): {message}");
            }
            if (found == 0)
            {
                Assert.Ignore("The strike levels are not built in this project.");
            }
            Assert.AreEqual(9, found, "All nine strike levels are built.");
        }
    }
}
