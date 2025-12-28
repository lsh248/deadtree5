using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    private NetworkGridManager gridManager;

    void Start()
    {
        gridManager = GetComponent<NetworkGridManager>();
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        // 접속 UI
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            GUILayout.BeginArea(new Rect(20, 20, 200, 200));
            if (GUILayout.Button("방 만들기 (Host)")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("참가하기 (Client)")) NetworkManager.Singleton.StartClient();
            GUILayout.EndArea();
            return;
        }

        // 승리 알림 및 돌 제거 버튼
        if (gridManager != null && gridManager.winner.Value != 0)
        {
            float w = 300; float h = 150;
            Rect rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
            GUILayout.BeginArea(rect, GUI.skin.box);

            string colorName = (gridManager.winner.Value == 1) ? "빨강" : "노랑";
            GUILayout.Label($"<size=25><b>{colorName} 승리!</b></size>", GUILayout.Height(50));

            if (GUILayout.Button("승리한 돌 제거하기", GUILayout.Height(60)))
            {
                // 서버에 돌 제거 요청 (누구나 누를 수 있게 설정)
                gridManager.RequestClearWinningPiecesRpc();
            }
            GUILayout.EndArea();
        }
    }
}
