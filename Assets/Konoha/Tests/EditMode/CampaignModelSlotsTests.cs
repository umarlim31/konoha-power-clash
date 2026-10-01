using System.Collections.Generic;
using Konoha.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Konoha.Tests
{
    // 0.3.1: owner model slots (Assets/Konoha/Art/Models/<Slot>/).
    public sealed class CampaignModelSlotsTests
    {
        [Test]
        public void SlotsAreUniqueWithUsableBoxesAndBudgets()
        {
            var names = new HashSet<string>();
            foreach (var slot in CampaignModelSlots.Slots)
            {
                Assert.That(names.Add(slot.name), Is.True, "Duplicate slot " + slot.name);
                Assert.That(slot.box.x > 0f && slot.box.y > 0f && slot.box.z > 0f, Is.True, slot.name);
                Assert.That(slot.maxTriangles, Is.GreaterThan(0), slot.name);
                Assert.That(slot.label, Is.Not.Empty, slot.name);
            }
            Assert.That(CampaignModelSlots.Find("Lentera"), Is.Not.Null);
            Assert.That(CampaignModelSlots.Find("Monumen"), Is.Null, "No slot for the route landmarks");
        }

        [Test]
        public void SettingsAcceptIndonesianDecimalsAndIgnoreJunk()
        {
            var settings = CampaignModelSlots.ParseSettings("skala=1,5\nputar: 90\nnaik = 0.2\nukuran=asli\nwarna=merah\nskala2");
            Assert.That(settings.scale, Is.EqualTo(1.5f).Within(.0001f));
            Assert.That(settings.yaw, Is.EqualTo(90f));
            Assert.That(settings.lift, Is.EqualTo(.2f).Within(.0001f));
            Assert.That(settings.originalSize, Is.True);

            var none = CampaignModelSlots.ParseSettings("");
            Assert.That(none.scale, Is.EqualTo(1f));
            Assert.That(none.originalSize, Is.False);
            Assert.That(CampaignModelSlots.ParseSettings("skala=0").scale, Is.EqualTo(1f), "A zero scale is ignored");
        }

        [Test]
        public void FitScaleKeepsTheModelInsideTheBox()
        {
            // A 100-unit tall tree in a 5 x 6.4 x 5 box: height decides.
            Assert.That(CampaignModelSlots.FitScale(new Vector3(40f, 100f, 40f), new Vector3(5f, 6.4f, 5f)), Is.EqualTo(.064f).Within(.0001f));
            // A wide flat house: width decides.
            Assert.That(CampaignModelSlots.FitScale(new Vector3(14f, 5f, 6f), new Vector3(7f, 5.3f, 6f)), Is.EqualTo(.5f).Within(.0001f));
            Assert.That(CampaignModelSlots.FitScale(Vector3.zero, Vector3.one), Is.EqualTo(1f));
        }

        [Test]
        public void MissingModelsChangeNothing()
        {
            CampaignModelSlots.Begin();
            var root = new GameObject("SlotRoot").transform;
            try
            {
                var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.transform.SetParent(root);
                Assert.That(CampaignModelSlots.Apply("Monumen", root, 0, Vector3.zero, 0f), Is.False, "Unknown slot");
                if (CampaignModelSlots.FindModelPath("Lentera") == null)
                {
                    Assert.That(CampaignModelSlots.Apply("Lentera", root, 0, Vector3.zero, 0f), Is.False);
                    Assert.That(CampaignModelSlots.Summary().Contains("Lentera x"), Is.False);
                }
                Assert.That(root.childCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(CampaignModelSlots.IsModelFile("Assets/x/Becak.FBX") && CampaignModelSlots.IsModelFile("a/b.obj"), Is.True);
                Assert.That(CampaignModelSlots.IsModelFile("a/b.blend"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }
    }
}
