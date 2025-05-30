using System;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using Steamworks;
using UnityEngine;

public struct ChatMessage : IBroadcast
{
    public string message;
    public string sender;
}

public class NetworkChatSystem : NetworkSingleton<NetworkChatSystem>
{
    public static event Action<ChatMessage> OnChatMessageRecived;

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerManager.RegisterBroadcast<ChatMessage>(ChatMessageRecived);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ServerManager.UnregisterBroadcast<ChatMessage>(ChatMessageRecived);
    }

    private void ChatMessageRecived(NetworkConnection connection, ChatMessage message, Channel channel)
    {
        message.sender = message.sender == "" ? connection.ClientId.ToString() : message.sender;

        string processedMessage = message.message
        .Replace("\u200B", "")  // Zero Width Space 제거
        .Replace("\n", "")
        .Replace("\r", "")
        .Trim()
        .ToLower();

        Debug.Log($"처리된 메시지: '{processedMessage}', 길이: {processedMessage.Length}");

        if(processedMessage == "/kill")
        {
            if(connection.FirstObject != null)
            {
                var player = connection.FirstObject.GetComponent<AlivePlayer>();
                if(player != null)
                {
                    player.TakeDamage(9999);
                }
            }
        }
        else{
            ServerManager.Broadcast(message, false, Channel.Reliable);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ClientManager.RegisterBroadcast<ChatMessage>(ChatMessageRecived);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        ClientManager.UnregisterBroadcast<ChatMessage>(ChatMessageRecived);
    }

    private void ChatMessageRecived(ChatMessage message, Channel channel)
    {
        OnChatMessageRecived?.Invoke(message);
    }

    public void SendChatMessage(string message)
    {
        ChatMessage chatMessage = new ChatMessage
        {
            message = message,
            sender = Managers.Network.Type == NetworkType.Steam ? SteamFriends.GetPersonaName() : ""
        };
        ClientManager.Broadcast(chatMessage);
    }
}
