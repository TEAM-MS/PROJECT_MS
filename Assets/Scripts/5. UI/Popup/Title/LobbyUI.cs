using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using System.Collections.Generic;

public class LobbyUI : PopupUI
{
    [Header("UI Prefab")]
    [SerializeField] private GameObject _createRoomUIPrefab;
    [SerializeField] private GameObject _lobbyRoomUIPrefab;
    [SerializeField] private GameObject _roomSlotUIPrefab;
 

    [Header("UI Button")]
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _refreshButton;
    [SerializeField] private CloseButton _closeButton;


    [Header("Room List Root")]
    [SerializeField] private GameObject _roomListRoot; 

    [Header("UI Root")]
    [SerializeField] private Transform _mainPanel;

    private CreateRoomUI _createRoomUI;
    private LobbyRoomUI _lobbyRoomUI;

    private List<RoomSlotUI> _roomSlotUIList = new List<RoomSlotUI>();

    protected override void Awake()
    {
        base.Awake();
        _createRoomButton.onClick.AddListener(OpenCreateRoomUI); 
        _closeButton.OnClick += Close;

        if(Managers.Network.Type == NetworkType.Steam)
        {
            InvokeRepeating(nameof(RefreshRoomList), 0.0f, 10.0f);
            _refreshButton.onClick.AddListener(RefreshRoomList); 
        }
    } 
 
    public override void Show()
    {
        base.Show();
        _mainPanel.localScale = Vector3.zero; 
        _mainPanel.DOScale(1.0f, 0.4f).SetEase(Ease.OutCubic); 
    }

    private void Close()
    { 
        _mainPanel.DOScale(0.0f, 0.4f).SetEase(Ease.OutCubic).onComplete += () => {
            Hide();  
        };  
    }

    private void OpenCreateRoomUI()
    {
        if (_createRoomUI == null)
        {
            _createRoomUI = Instantiate(_createRoomUIPrefab).GetComponent<CreateRoomUI>();
            _createRoomUI.Setup(_lobbyRoomUIPrefab);   
        }

        Managers.UI.ShowPopupUI(_createRoomUI);  
    }

    private void OpenLobbyRoomUI()
    {
        if (_lobbyRoomUI == null)
            _lobbyRoomUI = Instantiate(_lobbyRoomUIPrefab).GetComponent<LobbyRoomUI>();

        Managers.UI.ShowPopupUI(_lobbyRoomUI);   
    }

    private void RefreshRoomList()
    {
        RefreshRoomListAsync().Forget();
    }

    private async UniTaskVoid RefreshRoomListAsync()
    {
        var lobbyList = await Managers.Steam.RequestLobbyListAsync();

        if (lobbyList == null)
        {
            Debug.LogWarning("로비 목록을 가져올 수 없습니다.");
            return;
        }

        ResetRoomList();

        for(int i = 0; i < lobbyList.Count; i++)
        {
            var lobby = lobbyList[i];

            if(lobby.IsUnityNull()) continue;

            RoomSlotUI roomSlotUI = Instantiate(_roomSlotUIPrefab, _roomListRoot.transform).GetComponent<RoomSlotUI>();
            roomSlotUI.SetLobbyId(lobby.LobbyId.m_SteamID);
            roomSlotUI.UpdateUI(lobby);
            _roomSlotUIList.Add(roomSlotUI);
        }
    }

    private void ResetRoomList()
    {
        foreach (var roomSlotUI in _roomSlotUIList)
        {
            Destroy(roomSlotUI.gameObject);
        }

        _roomSlotUIList.Clear();
    }
}
