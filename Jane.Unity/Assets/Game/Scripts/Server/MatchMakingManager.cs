using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using TMPro;
using Michsky.UI.Reach;
using UnityEngine;

using MagicOnion;
using MagicOnion.Client;
using Cysharp.Threading.Tasks;

using Jane.Unity.ServerShared.Hubs;
using Jane.Unity.ServerShared.MemoryPackObjects;

public class MatchMakingManager : MonoBehaviour, IMatchMakingHubReceiver
{
    private readonly CancellationTokenSource shutdownCts = new();
    private IMatchMakingHub matchMakingHub;
    
    private GrpcChannelManager channelManager;
    private SceneManager sceneManager;

    [SerializeField] private TMP_Text profileText;
    [SerializeField] private ButtonManager lobbyPlayButton;
    [SerializeField] private ButtonManager lobbyStopSearchButton;
    [SerializeField] private TMP_Text lobbyPlayerCountText;
    [SerializeField] private List<MatchMakingLobbyUser> lobbyUsers;
    [SerializeField] private LobbyPlayer[] lobbyUserUIPanels;
    [SerializeField] private HotkeyEvent readyInputEvent;
    [SerializeField] private HotkeyEvent unReadyInputEvent;
    [SerializeField] private GameObject readyObject;
    [SerializeField] private GameObject unreadyObject;

    private void OnEnable()
    {
        lobbyPlayButton.onClick.AddListener(UniTask.UnityAction(async () => { await EnrollAsync(); }));
        lobbyStopSearchButton.onClick.AddListener(UniTask.UnityAction(async () => { await LeaveAsync(); }));
        readyInputEvent.onHotkeyPress.AddListener(UniTask.UnityAction(async () => { await ChangeReadyStateAsync(true); }));
        unReadyInputEvent.onHotkeyPress.AddListener(UniTask.UnityAction(async () => { await ChangeReadyStateAsync(false); }));

        channelManager = FindObjectOfType<GrpcChannelManager>();
        sceneManager = FindObjectOfType<SceneManager>();
    }

    private async UniTaskVoid Start()
    {
        await InitializeAsync();
    }

    private async UniTaskVoid OnDestroy()
    {
        lobbyPlayButton.onClick.RemoveAllListeners();
        lobbyStopSearchButton.onClick.RemoveAllListeners();
        readyInputEvent.onHotkeyPress.RemoveAllListeners();
        unReadyInputEvent.onHotkeyPress.RemoveAllListeners();
        
        shutdownCts.Cancel();

        if (matchMakingHub is not null)
        {
            if (GameInfo.GameId.Equals(Ulid.Empty)) await matchMakingHub.LeaveAsync().AsTask().AsUniTask();
            await matchMakingHub.DisposeAsync().AsUniTask();
        }
    }
    
    public async UniTask InitializeAsync()
    {
        while (!shutdownCts.IsCancellationRequested)
        {
            try
            {
                Debug.Log($"Connecting Server from MatchMakingManager...");
                matchMakingHub = await StreamingHubClient.ConnectAsync<IMatchMakingHub, IMatchMakingHubReceiver>(channelManager.GRPCChannel,
                    this,
                    cancellationToken: shutdownCts.Token).AsUniTask();
                Debug.Log($"MatchMakingHub connection established!");
                profileText.text = UserInfo.UserId;
                break;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            Debug.Log($"Failed to connect to the server. Retry after 5 seconds...");
            await UniTask.Delay(5 * 1000);
        }
    }

    private async UniTask EnrollAsync()
    {
        MatchMakingEnrollRequest request = new() { UserId = UserInfo.UserId, UniqueId = UserInfo.UniqueId, };
        try
        {
            MatchMakingEnrollResponse response = await matchMakingHub.EnrollAsync(request);
            Debug.Log(response.MatchId);

            // TODO: Validate lobbyUsers
            lobbyUsers = new(response.LobbyUsers);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
        
        for (int i = 0; i < lobbyUsers.Count; i++)
        {
            lobbyUserUIPanels[i].SetPlayerName(lobbyUsers[i].UserId);
            lobbyUserUIPanels[i].SetAdditionalText(lobbyUsers[i].UniqueId.ToString());
            lobbyUserUIPanels[i].SetState(lobbyUsers[i].IsReady ? LobbyPlayer.ItemState.Ready : LobbyPlayer.ItemState.NotReady);
        }

        readyObject.SetActive(true);
        unreadyObject.SetActive(false);
    }

    private async UniTask ChangeReadyStateAsync(bool isReady)
    {
        MatchMakingReadyRequest request = new() { UniqueId = UserInfo.UniqueId, IsReady = isReady };
        try
        {
            await matchMakingHub.ChangeReadyStateAsync(request);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        readyObject.SetActive(!isReady);
        unreadyObject.SetActive(isReady);
    }

    // OnLeave will confirm server side Leave
    private async UniTask LeaveAsync()
    {
        try
        {
            await matchMakingHub.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        readyObject.SetActive(false);
        unreadyObject.SetActive(false);
    }
    
    public void OnEnroll(MatchMakingLobbyUser user)
    {
        if (lobbyUsers.Any(lobbyUser => lobbyUser.UniqueId.Equals(user.UniqueId))) { return; }
        lobbyUsers.Add(user);

        var emptySlot = lobbyUserUIPanels.FirstOrDefault(panel => panel.currentState is LobbyPlayer.ItemState.Empty);

        emptySlot?.SetPlayerName(user.UserId);
        emptySlot?.SetAdditionalText(user.UniqueId.ToString());
        emptySlot?.SetState(user.IsReady ? LobbyPlayer.ItemState.Ready : LobbyPlayer.ItemState.NotReady);
    }

    public void OnLeave(MatchMakingLobbyUser leftUser)
    {
        if (leftUser.UniqueId.Equals(UserInfo.UniqueId))
        {
            lobbyUsers = null;
            foreach (var panel in lobbyUserUIPanels)
            {
                panel.SetPlayerName(string.Empty);
                panel.SetAdditionalText(string.Empty);
                panel.SetState(LobbyPlayer.ItemState.Empty);
            }
        }
        else
        {
            var otherUser = lobbyUsers?.FirstOrDefault(user => user.UniqueId.Equals(leftUser.UniqueId));
            lobbyUsers?.Remove(otherUser);

            var otherUserUI = lobbyUserUIPanels.FirstOrDefault(user => user.additionalText.Equals(leftUser.UniqueId.ToString()));
            otherUserUI?.SetPlayerName(string.Empty);
            otherUserUI?.SetAdditionalText(string.Empty);
            otherUserUI?.SetState(LobbyPlayer.ItemState.Empty);
        }
    }

    public void OnPlayerReadyStateChanged(MatchMakingReadyResponse response)
    {
        Debug.Log($"Received ULID: {response.UniqueId}, IsReady: {response.IsReady}");
        var lobbyUser = lobbyUsers.FirstOrDefault(user => user.UniqueId.Equals(response.UniqueId));
        if (lobbyUser is not null) { lobbyUser.IsReady = response.IsReady; }

        var lobbyUserUI = lobbyUserUIPanels.FirstOrDefault(user => user.additionalText.Equals(response.UniqueId.ToString()));
        if (lobbyUserUI is not null) { lobbyUserUI.SetState(response.IsReady ? LobbyPlayer.ItemState.Ready : LobbyPlayer.ItemState.NotReady); }
    }

    public void OnMatchMakingComplete(MatchMakingCompleteResponse response)
    {
        readyObject.SetActive(false);
        unreadyObject.SetActive(false);

        Debug.Log($"MatchMake Complete! GameID:{response.GameId}");

        lobbyUsers = null;
        foreach (var panel in lobbyUserUIPanels)
        {
            panel.SetPlayerName(string.Empty);
            panel.SetAdditionalText(string.Empty);
            panel.SetState(LobbyPlayer.ItemState.Empty);
        }

        GameInfo.GameId = response.GameId;
        GameInfo.PlayerCount = response.PlayerCount;
        // TODO: Fade
        // TODO: Load Game Scene
        sceneManager.LoadGameSceneAsync().Forget();
    }
}
