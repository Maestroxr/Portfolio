using System.Collections;
using Gamebox;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The language switch on the mission select: the flag is there, Hebrew redraws the words code writes and the fixed
    /// words of the canvas, and English brings them back.
    /// </summary>
    public class LanguageTest
    {
        private string originalCode;


        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (!string.IsNullOrEmpty(originalCode))
            {
                GameLanguages.Select(originalCode);
                yield return null;
            }
            originalCode = null;
        }


        [UnityTest]
        public IEnumerator TheMissionSelectSwitchesBetweenEnglishAndHebrew()
        {
            yield return TestScenes.Load(TestScenes.GameScene);
            float waited = 0f;
            while (!GameLanguages.IsReady && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (GameLanguages.Find(GameLanguages.Hebrew) == null)
            {
                Assert.Ignore("This project has no Hebrew locale.");
            }
            originalCode = GameLanguages.Code;
            AsteroidsGameManager manager = TestScenes.Manager;
            var ui = Object.FindAnyObjectByType<AsteroidsUI>();
            Assert.That(ui.titleScreen.GetComponentInChildren<LanguageButton>(true), Is.Not.Null, "The mission select has the flag.");
            TMP_Text hangar = ui.hangarButton.GetComponentInChildren<TMP_Text>(true);

            GameLanguages.Select(GameLanguages.English);
            yield return Settle(() => ui.launchLabel.text == "LAUNCH" || ui.launchLabel.text == "LOCKED");
            Assert.That(ui.launchLabel.text, Is.EqualTo("LAUNCH").Or.EqualTo("LOCKED"));
            Assert.That(hangar.text, Is.EqualTo("Hangar"));

            GameLanguages.Select(GameLanguages.Hebrew);
            yield return Settle(() => ui.launchLabel.text == "שיגור" || ui.launchLabel.text == "נעול");
            Assert.That(ui.launchLabel.text, Is.EqualTo("שיגור").Or.EqualTo("נעול"), "Code words redraw in Hebrew.");
            Assert.That(hangar.text, Is.EqualTo("האנגר"), "Fixed words follow the language.");
            Assert.That(ui.detailObjective.text, Does.Contain("מטרה"));

            GameLanguages.Select(GameLanguages.English);
            yield return Settle(() => ui.launchLabel.text == "LAUNCH" || ui.launchLabel.text == "LOCKED");
            Assert.That(ui.launchLabel.text, Is.EqualTo("LAUNCH").Or.EqualTo("LOCKED"), "English comes back.");
            Assert.That(hangar.text, Is.EqualTo("Hangar"));
        }


        /// <summary>Waits (up to 5 s) for the tables of the language picked to load and the screens to redraw.</summary>
        private static IEnumerator Settle(System.Func<bool> done)
        {
            float waited = 0f;
            while (!done() && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            yield return null;
        }
    }
}
