using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class NetworkGridManager : NetworkBehaviour
{
    [Header("말 프리팹 (0: 빨강, 1: 노랑)")]
    public GameObject[] playerPrefabs;

    private int[,] board = new int[10, 10];
    private GameObject[,] spawnedPieces = new GameObject[10, 10]; // 생성된 오브젝트 참조 저장

    public NetworkVariable<int> turnPlayer = new NetworkVariable<int>(1);
    public NetworkVariable<int> winner = new NetworkVariable<int>(0);

    // 승리에 기여한 좌표들을 저장할 리스트
    private List<Vector2Int> winningCoords = new List<Vector2Int>();

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (winner.Value != 0) return;

        int myID = NetworkManager.Singleton.IsHost ? 1 : 2;
        if (turnPlayer.Value != myID) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            int x = Mathf.FloorToInt(hit.point.x);
            int z = Mathf.FloorToInt(hit.point.z);

            if (x >= 0 && x < 10 && z >= 0 && z < 10 && board[x, z] == 0)
            {
                RequestPlacePieceRpc(x, z);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestPlacePieceRpc(int x, int z)
    {
        if (winner.Value != 0) return;

        int pID = turnPlayer.Value;
        SpawnPieceRpc(x, z, pID);

        if (CheckWin(x, z, pID))
        {
            winner.Value = pID;
        }
        else
        {
            turnPlayer.Value = (pID == 1) ? 2 : 1;
        }
    }

    [Rpc(SendTo.Everyone)]
    void SpawnPieceRpc(int x, int z, int pID)
    {
        board[x, z] = pID;
        if (IsServer)
        {
            Vector3 spawnPos = new Vector3(x + 0.5f, 0.1f, z + 0.5f);
            GameObject piece = Instantiate(playerPrefabs[pID - 1], spawnPos, Quaternion.identity);
            spawnedPieces[x, z] = piece; // 서버에서 참조 저장
            piece.GetComponent<NetworkObject>().Spawn();
        }
    }

    // --- 승리 돌 제거 로직 ---
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestClearWinningPiecesRpc()
    {
        foreach (var coord in winningCoords)
        {
            if (spawnedPieces[coord.x, coord.y] != null)
            {
                // 네트워크에서 제거 (모든 클라이언트 화면에서 사라짐)
                spawnedPieces[coord.x, coord.y].GetComponent<NetworkObject>().Despawn();
                board[coord.x, coord.y] = 0;
            }
        }
        winningCoords.Clear();
        winner.Value = 0; // 다시 게임 진행 가능 상태로
    }

    // --- 수정된 승리 판정 (좌표 수집 기능 추가) ---
    bool CheckWin(int x, int z, int pID)
    {
        int[,] dirs = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };
        bool hasWon = false;
        winningCoords.Clear();

        for (int i = 0; i < 4; i++)
        {
            List<Vector2Int> tempCoords = new List<Vector2Int>();
            tempCoords.Add(new Vector2Int(x, z));

            CollectInDirection(x, z, dirs[i, 0], dirs[i, 1], pID, tempCoords);
            CollectInDirection(x, z, -dirs[i, 0], -dirs[i, 1], pID, tempCoords);

            if (tempCoords.Count >= 4)
            {
                hasWon = true;
                // 승리한 모든 라인의 돌 좌표를 합침 (중복 제거)
                foreach (var c in tempCoords)
                {
                    if (!winningCoords.Contains(c)) winningCoords.Add(c);
                }
            }
        }
        return hasWon;
    }

    void CollectInDirection(int x, int z, int dx, int dz, int pID, List<Vector2Int> list)
    {
        int nx = x + dx;
        int nz = z + dz;
        while (nx >= 0 && nx < 10 && nz >= 0 && nz < 10 && board[nx, nz] == pID)
        {
            list.Add(new Vector2Int(nx, nz));
            nx += dx;
            nz += dz;
        }
    }
}