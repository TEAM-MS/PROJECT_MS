using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NetworkGameSystem : NetworkSingleton<NetworkGameSystem>
{
    public readonly SyncVar<bool> IsGameStarted = new SyncVar<bool>(false);
    public readonly SyncVar<GameOptions> GameOptions = new SyncVar<GameOptions>(new GameOptions(1, 1, 3));
    public readonly SyncDictionary<NetworkConnection, PlayerInfo> Players = new SyncDictionary<NetworkConnection, PlayerInfo>();
    public readonly SyncList<NetworkConnection> Imposters = new SyncList<NetworkConnection>();
    [SerializeField] private NetworkObject ghostPlayerPrefab;

    [Server]
    public void StartGame()
    {
        IsGameStarted.Value = true;

        SetRandomRole();

        NetworkSceneSystem.Instance?.LoadScene("Game");
    }

    [Server]
    public void EndGame(PlayerRole winner)
    {
        IsGameStarted.Value = false;

        NetworkSceneSystem.Instance?.LoadScene("Lobby");
    }

    [Server]
    public void SetGameOptions(GameOptions options)
    {
        GameOptions.Value = options;
    }

    [Server]
    private void SetRandomRole()
    {
        Players.Clear();
        Imposters.Clear();

        Imposters.AddRange(InstanceFinder.ServerManager.Clients.Values
            .OrderBy(x => Random.value)
            .Take(GameOptions.Value.imposterCount)
            .ToList());

        foreach(NetworkConnection connection in InstanceFinder.ServerManager.Clients.Values)
        {
            PlayerRole role = Imposters.Contains(connection) ? PlayerRole.Imposter : PlayerRole.Survival;
            PlayerInfo playerInfo = new PlayerInfo(role, false);
            Debug.Log(playerInfo);
            Players.Add(connection, playerInfo);
        }
    }

    public PlayerRole GetPlayerRole(NetworkConnection connection)
    {
        if(Players.TryGetValue(connection, out PlayerInfo playerInfo))
        {
            return playerInfo.role;
        }
        return PlayerRole.Survival;
    }

    public void ImposterWin()
    {
        Debug.Log("Imposter Win");
        EndGame(PlayerRole.Imposter);
    }

    public void SurvivalWin()
    {
        Debug.Log("Survival Win");
        EndGame(PlayerRole.Survival);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerDead(Vector3 position, NetworkConnection connection = null)
    {
        if(Players.TryGetValue(connection, out PlayerInfo playerInfo))
        {
            playerInfo.isDead = true;
            Players[connection] = playerInfo;
        }

        int aliveSurvivals = Players.Count(player => player.Value.role == PlayerRole.Survival && !player.Value.isDead);

        // if(aliveSurvivals <= 0)
        // {
        //     ImposterWin();
        // }
        // else
        // {
        //     NetworkObject instance = Instantiate(ghostPlayerPrefab, position, Quaternion.identity);
        //     InstanceFinder.ServerManager.Spawn(instance, connection);
        // }

        NetworkObject instance = Instantiate(ghostPlayerPrefab, position, Quaternion.identity);
        InstanceFinder.ServerManager.Spawn(instance, connection);
    }
}

public struct GameOptions
{
    public int imposterCount;
    /// <summary>
    /// expression in seconds
    /// </summary>
    public int dayDuration;
    public int maxDayCount;

    public GameOptions(int imposterCount, int dayDuration, int maxDayCount)
    {
        this.imposterCount = imposterCount;
        this.dayDuration = dayDuration;
        this.maxDayCount = maxDayCount;
    }
}

public struct PlayerInfo
{
    public PlayerRole role;
    public bool isDead;

    public PlayerInfo(PlayerRole role, bool isDead)
    {
        this.role = role;
        this.isDead = isDead;
    }
}

public enum PlayerRole
{
    Survival,
    Imposter,
}