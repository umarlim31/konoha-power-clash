using Konoha.Campaign;
using NUnit.Framework;

namespace Konoha.Tests
{
    // §8.2 Biro Prosedur "Sahkan Berkas" rules (pure logic, the host applies the changes).
    public sealed class BiroEncounterTests
    {
        private const float Frame = 1f / 60f;

        private static BiroChange Stand(BiroEncounter biro, int loket, bool contested, float seconds)
        {
            BiroChange all = BiroChange.None;
            for (float t = 0f; t < seconds - Frame * 0.5f; t += Frame)
                all |= biro.TickLokets(loket, contested, Frame);
            return all;
        }

        [Test]
        public void LoketStampsAfterThreeSecondsInside()
        {
            var biro = new BiroEncounter();
            Stand(biro, 0, false, 2.9f);
            Assert.That(biro.IsStamped(0), Is.False);
            BiroChange change = Stand(biro, 0, false, 0.15f);
            Assert.That(change & BiroChange.LoketStamped, Is.EqualTo(BiroChange.LoketStamped));
            Assert.That(biro.IsStamped(0), Is.True);
            Assert.That(biro.StampedCount, Is.EqualTo(1));
        }

        [Test]
        public void EnemyInsideTheLoketPausesProgress()
        {
            var biro = new BiroEncounter();
            Stand(biro, 1, false, 1.5f);
            float before = biro.Progress(1);
            Stand(biro, 1, true, 5f);
            Assert.That(biro.Progress(1), Is.EqualTo(before).Within(0.0001f), "ANTRIAN pauses, never loses");
            Assert.That(biro.IsStamped(1), Is.False);
        }

        [Test]
        public void LeftLoketDecaysTwentyPercentPerSecond()
        {
            var biro = new BiroEncounter();
            Stand(biro, 2, false, 1.5f); // 50%
            Stand(biro, -1, false, 1f);
            Assert.That(biro.Progress(2), Is.EqualTo(0.30f).Within(0.01f));
            Stand(biro, 0, false, 3f);
            Assert.That(biro.Progress(2), Is.Zero, "Never below zero");
        }

        [Test]
        public void StampedLoketStaysAndAllThreeOpenTheDoor()
        {
            var biro = new BiroEncounter();
            Stand(biro, 0, false, 3.05f);
            Stand(biro, -1, false, 10f);
            Assert.That(biro.IsStamped(0), Is.True);
            Stand(biro, 1, false, 3.05f);
            BiroChange last = Stand(biro, 2, false, 3.05f);
            Assert.That(last & BiroChange.AllStamped, Is.EqualTo(BiroChange.AllStamped));
            Assert.That(biro.AllStamped, Is.True);
            Assert.That(biro.TickLokets(0, false, Frame), Is.EqualTo(BiroChange.None));
        }

        [Test]
        public void ArsipReturnsEveryTwelveSecondsUntilTheLoketsAreDone()
        {
            var biro = new BiroEncounter();
            Assert.That(biro.TickArsip(2, 2, 20f), Is.Zero, "Full: no respawn and no stored time");
            Assert.That(biro.TickArsip(1, 2, 11.9f), Is.Zero);
            Assert.That(biro.TickArsip(1, 2, 0.2f), Is.EqualTo(1));
            Assert.That(biro.TickArsip(1, 2, 1f), Is.Zero, "Clock restarts after a spawn");

            for (int i = 0; i < 3; i++) Stand(biro, i, false, 3.05f);
            Assert.That(biro.TickArsip(0, 2, 30f), Is.Zero, "No more arsip after the last stamp");
        }

        [Test]
        public void ClearedNeedsStampsAndTheKepalaBiro()
        {
            var biro = new BiroEncounter();
            Assert.That(biro.Evaluate(false), Is.EqualTo(BiroChange.LeaderDown), "Kepala down early is not enough");
            Assert.That(biro.Cleared, Is.False);
            for (int i = 0; i < 3; i++) Stand(biro, i, false, 3.05f);
            Assert.That(biro.Evaluate(false), Is.EqualTo(BiroChange.Cleared));
            Assert.That(biro.Evaluate(false), Is.EqualTo(BiroChange.None));

            biro.Reset();
            Assert.That(biro.StampedCount, Is.Zero);
            Assert.That(biro.Progress(0), Is.Zero);
            Assert.That(biro.Cleared, Is.False);
        }

        [Test]
        public void StempelTundaSlowsSkillsByThirtyPercent()
        {
            Assert.That(BiroEncounter.CooldownMultiplier(true), Is.EqualTo(1.30f).Within(0.0001f));
            Assert.That(BiroEncounter.CooldownMultiplier(false), Is.EqualTo(1f));
        }
    }
}
