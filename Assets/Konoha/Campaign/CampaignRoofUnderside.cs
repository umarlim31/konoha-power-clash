using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.3: where the room under a roof really ends (world height), written by the generator.
    // The upper half of an owner model (CampaignModelSlots "... atas") also holds the tops of
    // the pillars, so its renderer bounds reach down to the floor: in 0.6.1/0.6.2 the pendopo
    // never counted as a roof and was hidden instead of the camera coming inside.
    public sealed class CampaignRoofUnderside : MonoBehaviour
    {
        public float worldY;

        // The underside of a roof renderer: the generator's value, else the bottom of its bounds.
        public static float Of(Renderer roof)
        {
            if (roof == null)
                return 0f;
            return roof.TryGetComponent(out CampaignRoofUnderside mark) ? mark.worldY : roof.bounds.min.y;
        }
    }
}
