using Konoha.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Core
{
    // Single-player route validation: three offices, then the seat. No combat or multiplayer claim.
    public sealed class PowerRoute : MonoBehaviour
    {
        public CharacterMotor player;
        public InstitutionObjective[] offices;
        public Transform throne;
        public Renderer throneIndicator;
        public Text status;
        public float throneRadius = 2.1f;
        public float throneSeconds = 4f;
        private float throneProgress;
        private bool won;

        private void Update()
        {
            if (player == null || offices == null || throne == null || status == null || won) return;
            int captured = 0;
            InstitutionObjective active = null;
            foreach (var office in offices)
            {
                if (office == null) continue;
                office.Tick(player.transform.position, Time.deltaTime);
                if (office.Captured) captured++;
                else if (office.Progress > 0f) active = office;
            }

            if (captured < offices.Length)
            {
                throneProgress = 0f;
                status.text = active != null
                    ? $"{active.displayName}: {active.Progress / active.captureSeconds:P0}  |  Institusi {captured}/{offices.Length}\nTetap di lingkaran untuk menguasai"
                    : $"JALUR KEKUASAAN  |  Institusi {captured}/{offices.Length}\nKuasai KPU, Komisi, dan Parlemen sebelum menuju takhta";
                return;
            }

            if (throneIndicator != null) throneIndicator.material.SetColor("_BaseColor", new Color(0.96f, 0.68f, 0.2f));
            Vector2 offset = new Vector2(player.transform.position.x - throne.position.x,
                player.transform.position.z - throne.position.z);
            throneProgress = offset.sqrMagnitude <= throneRadius * throneRadius
                ? Mathf.Min(throneSeconds, throneProgress + Time.deltaTime) : 0f;
            won = throneProgress >= throneSeconds;
            status.text = won ? "TAKHTA DIKUASAI  |  Rute prototipe selesai" :
                $"SEMUA INSTITUSI DIKUASAI  |  Takhta {throneProgress / throneSeconds:P0}\nDekati takhta dan bertahan di lingkaran";
        }
    }
}
