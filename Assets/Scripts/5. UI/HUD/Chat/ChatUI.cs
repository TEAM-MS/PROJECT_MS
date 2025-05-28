using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatMessagePrefab; // 채팅  UI 프리팹
    [SerializeField] private Transform chatRoot; // 채팅 부모 객체
    [SerializeField] private TMP_InputField chatInput; // 채팅 입력창
    [SerializeField] private TMP_Text chatText; // 채팅 텍스트
    [SerializeField] private string playerNickname = "사람1"; // 플레이어 닉네임

    [Header("Settings")]
    [SerializeField] private int maxMessages = 8; // 최대 표시할 채팅 수

    private bool inputActive = false;
    private List<GameObject> chatMessages = new List<GameObject>();

    private static readonly string _clickSound = "Sound/UI/Click_06.mp3";

    private void Start()
    {
        NetworkChatSystem.OnChatMessageRecived += DisplayMessage;
    }

    private void OnDestroy()
    {
        NetworkChatSystem.OnChatMessageRecived -= DisplayMessage;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) // Enter 키 처리
        {
            if (!inputActive)
            {
                chatInput.gameObject.SetActive(true); // 채팅 입력창 활성화
                chatInput.ActivateInputField();
                inputActive = true;
                Managers.Input.SetInputActive(false);
            }
            else
            {
                string text = chatText.text.Replace("<u>", "").Replace("</u>", "");
                if (!string.IsNullOrWhiteSpace(text)) // 채팅 입력창에 내용이 있으면 전송
                {
                    SendChatMessage(text);
                    chatInput.text = "";
                }

                chatInput.DeactivateInputField(); // 채팅 입력창 비활성화
                chatInput.gameObject.SetActive(false);
                inputActive = false;
                Managers.Input.SetInputActive(true);
            }
        }
    }

    public void SendChatMessage(string message) // 채팅 전송 및 표시
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Managers.Sound.Play(_clickSound);
        NetworkChatSystem.Instance.SendChatMessage(message);
        Managers.Analytics.ChatUsage();
    }

    public void SetNickname(string nickname) // 닉네임 설정
    {
        if (!string.IsNullOrWhiteSpace(nickname))
        {
            playerNickname = nickname;
        }
    }

    public void DisplayMessage(ChatMessage message) // 채팅 UI에 표시
    {
        string formattedMessage = $"[{message.sender}] {message.message}";

        GameObject chatObj = Instantiate(chatMessagePrefab, chatRoot); // 새 채팅 생성
        TextMeshProUGUI textComponent = chatObj.GetComponentInChildren<TextMeshProUGUI>();
        textComponent.text = formattedMessage;

        chatMessages.Add(chatObj); // 채팅 목록에 추가

        if (chatMessages.Count > maxMessages) // 최대 채팅 수 초과 시 오래된 채팅 제거
        {
            GameObject oldestMessage = chatMessages[0];
            chatMessages.RemoveAt(0);
            Destroy(oldestMessage);
        }
    }
}
