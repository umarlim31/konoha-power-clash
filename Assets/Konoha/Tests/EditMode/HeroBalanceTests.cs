using System;
using System.Linq;
using Konoha.Networking;
using NUnit.Framework;

namespace Konoha.Tests
{
    // 0.2.4: the four heroes are balanced for Jalur Takhta, and PvP keeps its original numbers.
    public sealed class HeroBalanceTests
    {
        private static PrototypeHero[] Heroes => (PrototypeHero[])Enum.GetValues(typeof(PrototypeHero));

        [Test]
        public void SoloHeroesPutOutSimilarPressure()
        {
            float[] pressure = Heroes.Select(h => HeroBalance.EstimatedPressure(h, true)).ToArray();
            float mean = pressure.Average();
            for (int i = 0; i < pressure.Length; i++)
                Assert.That(pressure[i] / mean, Is.InRange(.88f, 1.12f),
                    Heroes[i] + " is out of balance: " + pressure[i] + " vs mean " + mean);
        }

        [Test]
        public void PakWiIsNoLongerTheWeakestByFar()
        {
            float pakWi = HeroBalance.EstimatedPressure(PrototypeHero.Jokowi, true);
            float before = HeroBalance.EstimatedPressure(PrototypeHero.Jokowi, false);
            Assert.That(pakWi, Is.GreaterThan(before * 1.5f));
            foreach (PrototypeHero hero in Heroes)
                Assert.That(pakWi, Is.GreaterThan(HeroBalance.EstimatedPressure(hero, true) * .9f), hero.ToString());
        }

        [Test]
        public void PvpKeepsTheOriginalNumbers()
        {
            Assert.That(HeroBalance.BasicDamage(PrototypeHero.Mega, false), Is.EqualTo(18));
            Assert.That(HeroBalance.BasicDamage(PrototypeHero.Prabowo, false), Is.EqualTo(24));
            Assert.That(HeroBalance.BasicDamage(PrototypeHero.Abah, false), Is.EqualTo(17));
            Assert.That(HeroBalance.BasicDamage(PrototypeHero.Jokowi, false), Is.EqualTo(15));
            Assert.That(HeroBalance.BasicRange(PrototypeHero.Abah), Is.EqualTo(6.8f));
            Assert.That(HeroBalance.BasicRange(PrototypeHero.Prabowo), Is.EqualTo(2.8f));
            Assert.That(HeroBalance.BasicRange(PrototypeHero.Mega), Is.EqualTo(2.7f));
            Assert.That(HeroBalance.BasicCooldown(PrototypeHero.Prabowo, false), Is.EqualTo(.88f));
            Assert.That(HeroBalance.BasicCooldown(PrototypeHero.Prabowo, true), Is.EqualTo(.62f));
            Assert.That(HeroBalance.BasicCooldown(PrototypeHero.Abah, false), Is.EqualTo(.72f));
            Assert.That(HeroBalance.BasicCooldown(PrototypeHero.Jokowi, false), Is.EqualTo(.68f));
            Assert.That(HeroBalance.BasicCooldown(PrototypeHero.Mega, false), Is.EqualTo(.64f));
            Assert.That(HeroBalance.PvpBarisDamage, Is.EqualTo(10));
            Assert.That(HeroBalance.PvpPidatoDamage, Is.EqualTo(18));
        }
    }
}
