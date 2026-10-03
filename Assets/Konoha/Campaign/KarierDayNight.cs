using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.3 KARIER day and night (owner: "siklus siang dan malam"). KarierController sets the
    // clock (minutes after midnight) from KarierLife; this blends the generator's daylight
    // (sun, fill light, trilight ambient, fog, procedural sky) with a moonlit night, and
    // switches on the warm lamp glows (unlit spheres, no realtime lights: Android budget).
    // Nothing changes until KARIER starts; Restore puts the generator's daylight back.
    public sealed class KarierDayNight : MonoBehaviour
    {
        public Light sun;
        public Light fill;
        public GameObject lampGlows;
        public Color nightSun = new Color(.55f, .66f, 1f);
        public float nightSunIntensity = .32f;
        public Color nightSky = new Color(.12f, .16f, .30f);
        public Color nightEquator = new Color(.10f, .11f, .17f);
        public Color nightGround = new Color(.05f, .05f, .07f);
        public Color nightFog = new Color(.07f, .09f, .16f);
        public Color duskSun = new Color(1f, .62f, .36f);

        private bool captured, active;
        private Quaternion dayRotation;
        private Color daySun, daySky, dayEquator, dayGround, dayFog, daySkyTint;
        private float dayIntensity, dayFill, dayExposure;
        private Material originalSky, skyCopy;
        private float lastApplied = -1f;

        public bool IsNight { get; private set; }

        private void Capture()
        {
            if (captured)
                return;
            captured = true;
            if (sun != null)
            {
                dayRotation = sun.transform.rotation;
                daySun = sun.color;
                dayIntensity = sun.intensity;
            }
            if (fill != null)
                dayFill = fill.intensity;
            daySky = RenderSettings.ambientSkyColor;
            dayEquator = RenderSettings.ambientEquatorColor;
            dayGround = RenderSettings.ambientGroundColor;
            dayFog = RenderSettings.fogColor;
            originalSky = RenderSettings.skybox;
            if (originalSky != null)
            {
                // A copy, so the generated sky asset itself is never changed.
                skyCopy = new Material(originalSky);
                dayExposure = skyCopy.HasProperty("_Exposure") ? skyCopy.GetFloat("_Exposure") : 1f;
                daySkyTint = skyCopy.HasProperty("_SkyTint") ? skyCopy.GetColor("_SkyTint") : Color.white;
                RenderSettings.skybox = skyCopy;
            }
        }

        // 1 in full day, 0 at night; dawn 05:00-06:45, dusk 17:30-19:00.
        public static float Daylight(float clock)
        {
            float hour = clock / 60f;
            if (hour >= 6.75f && hour <= 17.5f) return 1f;
            if (hour >= 5f && hour < 6.75f) return Mathf.SmoothStep(0f, 1f, (hour - 5f) / 1.75f);
            if (hour > 17.5f && hour <= 19f) return Mathf.SmoothStep(1f, 0f, (hour - 17.5f) / 1.5f);
            return 0f;
        }

        public void SetClock(float clock)
        {
            Capture();
            active = true;
            // The light only needs to move when the clock moved a little (shadows stay calm).
            if (lastApplied >= 0f && Mathf.Abs(clock - lastApplied) < 1.5f)
                return;
            lastApplied = clock;
            float day = Daylight(clock);
            float hour = clock / 60f;
            // Low, warm sun at the edges of the day; overhead at noon. The moon comes from the
            // other side at night.
            float dawnDusk = 1f - Mathf.Clamp01(Mathf.Abs(hour - 12.2f) / 5.8f);
            if (sun != null)
            {
                float pitch = Mathf.Lerp(14f, 62f, dawnDusk);
                Vector3 dayEuler = dayRotation.eulerAngles;
                Quaternion daySunRotation = Quaternion.Euler(pitch, dayEuler.y + (hour - 12f) * 7f, 0f);
                Quaternion moon = Quaternion.Euler(52f, dayEuler.y + 150f, 0f);
                sun.transform.rotation = Quaternion.Slerp(moon, daySunRotation, day);
                Color warm = Color.Lerp(duskSun, daySun, Mathf.Clamp01(dawnDusk * 2.2f));
                sun.color = Color.Lerp(nightSun, warm, day);
                sun.intensity = Mathf.Lerp(nightSunIntensity, dayIntensity, day);
            }
            if (fill != null)
                fill.intensity = Mathf.Lerp(dayFill * .35f, dayFill, day);
            RenderSettings.ambientSkyColor = Color.Lerp(nightSky, daySky, day);
            RenderSettings.ambientEquatorColor = Color.Lerp(nightEquator, dayEquator, day);
            RenderSettings.ambientGroundColor = Color.Lerp(nightGround, dayGround, day);
            RenderSettings.fogColor = Color.Lerp(nightFog, dayFog, day);
            if (skyCopy != null)
            {
                if (skyCopy.HasProperty("_Exposure"))
                    skyCopy.SetFloat("_Exposure", Mathf.Lerp(.16f, dayExposure, day));
                if (skyCopy.HasProperty("_SkyTint"))
                    skyCopy.SetColor("_SkyTint", Color.Lerp(new Color(.16f, .20f, .38f), daySkyTint, day));
            }
            IsNight = day < .45f;
            if (lampGlows != null && lampGlows.activeSelf != IsNight)
                lampGlows.SetActive(IsNight);
        }

        public void Restore()
        {
            if (!captured || !active)
                return;
            active = false;
            lastApplied = -1f;
            if (sun != null)
            {
                sun.transform.rotation = dayRotation;
                sun.color = daySun;
                sun.intensity = dayIntensity;
            }
            if (fill != null)
                fill.intensity = dayFill;
            RenderSettings.ambientSkyColor = daySky;
            RenderSettings.ambientEquatorColor = dayEquator;
            RenderSettings.ambientGroundColor = dayGround;
            RenderSettings.fogColor = dayFog;
            if (originalSky != null)
                RenderSettings.skybox = originalSky;
            if (skyCopy != null)
            {
                Destroy(skyCopy);
                skyCopy = null;
            }
            captured = false;
            IsNight = false;
            if (lampGlows != null)
                lampGlows.SetActive(false);
        }

        private void OnDestroy() => Restore();
    }
}
