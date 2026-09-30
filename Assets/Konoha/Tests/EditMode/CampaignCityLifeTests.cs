using Konoha.Campaign;
using NUnit.Framework;
using UnityEngine;

namespace Konoha.Tests
{
    // 0.2.2 Kota Hidup: the traffic and fountain maths (pure, no scene).
    public sealed class CampaignCityLifeTests
    {
        private static readonly Vector3[] Square =
        {
            new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f), new Vector3(0f, 0f, 10f)
        };

        [Test]
        public void RingLengthIsThePerimeter()
        {
            Assert.That(CampaignCityLife.RingLength(Square), Is.EqualTo(40f).Within(.001f));
            Assert.That(CampaignCityLife.RingLength(new Vector3[0]), Is.Zero);
        }

        [Test]
        public void RingPointWalksTheCornersAndWraps()
        {
            Vector3 p = CampaignCityLife.RingPoint(Square, 15f, out Vector3 heading);
            Assert.That(Vector3.Distance(p, new Vector3(10f, 0f, 5f)), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(heading, Vector3.forward), Is.LessThan(.001f));
            Vector3 wrapped = CampaignCityLife.RingPoint(Square, 55f, out _);
            Assert.That(Vector3.Distance(wrapped, p), Is.LessThan(.001f));
            Vector3 negative = CampaignCityLife.RingPoint(Square, -25f, out _);
            Assert.That(Vector3.Distance(negative, p), Is.LessThan(.001f));
        }

        [Test]
        public void IndonesianTrafficKeepsLeft()
        {
            // Counter-clockwise on the south road drives east; left of east is north (+z),
            // the same formula CampaignCityLife uses for the lane offset.
            CampaignCityLife.RingPoint(Square, 5f, out Vector3 heading);
            var left = new Vector3(-heading.z, 0f, heading.x);
            Assert.That(left.z, Is.GreaterThan(.99f));
        }

        [Test]
        public void LaneRingsShiftInwardForwardAndOutwardBackward()
        {
            Vector3[] inner = CampaignCityLife.LaneRing(Square, 1f);
            Vector3[] outer = CampaignCityLife.LaneRing(Square, -1f);
            Assert.That(Vector3.Distance(inner[0], new Vector3(1f, 0f, 1f)), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(inner[2], new Vector3(9f, 0f, 9f)), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(outer[0], new Vector3(-1f, 0f, -1f)), Is.LessThan(.001f));
            Assert.That(CampaignCityLife.RingLength(inner), Is.EqualTo(32f).Within(.001f));
        }

        [Test]
        public void WrapRoadStaysOnTheRoad()
        {
            Assert.That(CampaignCityLife.WrapRoad(105f, 100f), Is.EqualTo(-95f).Within(.001f));
            Assert.That(CampaignCityLife.WrapRoad(-101f, 100f), Is.EqualTo(99f).Within(.001f));
            Assert.That(CampaignCityLife.WrapRoad(12f, 100f), Is.EqualTo(12f).Within(.001f));
        }

        [Test]
        public void DropletsArcAboveTheNozzleAndStayNearTheBasin()
        {
            var nozzle = new Vector3(10f, 1f, 5f);
            for (int i = 0; i < 6; i++)
            {
                for (float t = 0f; t < 3f; t += .1f)
                {
                    Vector3 drop = CampaignCityLife.DropletPosition(nozzle, i, 6, t, .45f, .55f);
                    Vector2 flat = new Vector2(drop.x - nozzle.x, drop.z - nozzle.z);
                    Assert.That(flat.magnitude, Is.LessThanOrEqualTo(.451f));
                    Assert.That(drop.y, Is.InRange(nozzle.y - CampaignCityLife.NozzleHeight - .01f, nozzle.y + .56f));
                }
            }
        }
    }
}
