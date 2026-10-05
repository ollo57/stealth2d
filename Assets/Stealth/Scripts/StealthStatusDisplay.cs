using UnityEngine;

namespace AlleyStealth
{
    public sealed class StealthStatusDisplay : MonoBehaviour
    {
        [SerializeField] private FreeMovementController player;
        [SerializeField] private float finishX = 17f;
        [SerializeField] private EnemyVision[] lookouts;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle stateStyle;

        private void OnGUI()
        {
            if (player == null) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
                bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                stateStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold };
            }
            float scale = Mathf.Min(Screen.width / 1000f, Screen.height / 650f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.Box(new Rect(20, 20, 620, 142), GUIContent.none);
            GUI.Label(new Rect(36, 29, 585, 36), "THE QUIET WAY  /  Night watch", titleStyle);
            bool detected = false;
            bool blockedByCover = false;
            foreach (EnemyVision lookout in lookouts)
            {
                if (lookout == null || !lookout.isActiveAndEnabled) continue;
                detected |= lookout.CanSeePlayer;
                blockedByCover |= lookout.PlayerInViewSector && !lookout.CanSeePlayer;
            }
            stateStyle.normal.textColor = detected ? new Color(1f, 0.35f, 0.35f) : new Color(0.4f, 1f, 0.75f);
            string status = detected ? "SPOTTED  /  get behind cover" :
                blockedByCover ? "OCCLUDED  /  cover blocks sight" : "UNSEEN  /  outside their sight";
            GUI.Label(new Rect(36, 65, 585, 28), status, stateStyle);
            string hint = "WASD / arrows: move freely. Circle obstacles and stay out of the red sight areas.";
            if (player.transform.position.x >= finishX)
                hint = "You reached the far gate. Try a different route through the courtyard.";
            GUI.Label(new Rect(36, 99, 585, 54), hint, bodyStyle);
            GUI.matrix = previousMatrix;
        }
    }
}
