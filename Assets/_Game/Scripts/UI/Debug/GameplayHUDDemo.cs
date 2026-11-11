using System.Collections;
using UnityEngine;

namespace FishJam.UI
{
    /// <summary>
    /// Bang TEST tren HUD (chi Editor / Development Build). Gia lap gameplay:
    ///  - "+3 ca": 1 be day -> OnTankCompleted() (combo, chu khen) + tang bo dem 3
    ///  - "Du ca": dat bo dem = tong
    ///  - du ca -> cho 1 giay -> mo man Thang (Win)
    ///  - Chu khen ke tiep, Sap thua (do), Tu dong xong be
    /// An khi popup Setting / Revive / Win / Lose dang mo. Thua -> Hoi sinh: mo Revive (X -> Lose).
    /// </summary>
    [RequireComponent(typeof(GamePlay))]
    public class GameplayHUDDemo : MonoBehaviour
    {
        public bool showPanel = true;
        public bool autoPraise = false;
        public float praiseEvery = 2.6f;
        public int total = 30;

        GamePlay hud; int tier, fish; float next; bool warn, ending;

        void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false; return;
#endif
            hud = GetComponent<GamePlay>();
        }

        // Moi lan HUD mo (vao man / choi lai): reset bo dem
        void OnEnable()
        {
            if (hud == null) return;
            fish = 0; ending = false; warn = false; next = Time.time + 1f;
            hud.SetFishCount(fish, total);
        }

        void Update()
        {
            if (!autoPraise || ending || Time.time < next) return;
            next = Time.time + praiseEvery;
            TankFull();
        }

        void TankFull() { hud.OnTankCompleted(); AddFish(3); }

        void AddFish(int n)
        {
            if (ending) return;
            fish = Mathf.Min(total, fish + n);
            hud.SetFishCount(fish, total);
            if (fish >= total) { ending = true; StartCoroutine(EndLevel()); }
        }

        IEnumerator EndLevel()
        {
            yield return new WaitForSecondsRealtime(1f);
            if (GameManager.Exists) UIManager.Instance.Open<Win>();                            // man Thang
            else OnEnable();                                                                   // scene thu UI: chi reset
        }

        void Praise() { hud.ShowPraise(tier); tier = (tier + 1) % hud.praises.Length; }

        void OnGUI()
        {
            if (!showPanel || hud == null || Setting.IsShowing) return;
            if (UIManager.Exists && (UIManager.Instance.IsOpened<Win>() || UIManager.Instance.IsOpened<Lose>() || UIManager.Instance.IsOpened<Revive>())) return;
            float s = Screen.height / 1100f, w = 170 * s, h = 40 * s;
            var st = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(15 * s) };
            GUILayout.BeginArea(new Rect(Screen.width - w - 10 * s, Screen.height * .5f, w, h * 8f), GUI.skin.box);
            GUILayout.Label("TEST  " + fish + "/" + total, new GUIStyle(GUI.skin.label) { fontSize = st.fontSize, alignment = TextAnchor.MiddleCenter });
            GUI.enabled = !ending;
            autoPraise = GUILayout.Toggle(autoPraise, " Tu dong xong be", new GUIStyle(GUI.skin.toggle) { fontSize = st.fontSize });
            if (GUILayout.Button("+3 ca (combo " + hud.Combo + ")", st, GUILayout.Height(h * .8f))) TankFull();
            if (GUILayout.Button("Du ca", st, GUILayout.Height(h * .8f))) AddFish(total);
            if (GUILayout.Button("Chu khen ke tiep", st, GUILayout.Height(h * .8f))) Praise();
            if (GUILayout.Button("Thua -> Hoi sinh", st, GUILayout.Height(h * .8f))) { var r = UIManager.Instance.Open<Revive>(); if (r) r.SetProgress((float)fish / total); }
            if (GUILayout.Button("Thua (bo qua hoi sinh)", st, GUILayout.Height(h * .8f))) UIManager.Instance.Open<Lose>();
            if (GUILayout.Button(warn ? "Tat sap thua" : "Sap thua (do)", st, GUILayout.Height(h * .8f))) { warn = !warn; hud.SetWarning(warn); }
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
}
