//using Td = Telegram.Td;
//using TdApi = Telegram.Td.Api;
using TdLib;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

public class unity_telegram_manager : MonoBehaviour
{
    private static readonly int ApiId = 00000000;
    private static readonly string ApiHash = "0000000000000000000000000000000";
    private static readonly string PhoneNumber = "+0000000000000"; // must contain prefix

    private static readonly ManualResetEventSlim ResetEvent = new ManualResetEventSlim();
    private static bool _authNeeded;
    private static TdClient _client;

    // Start is called before the first frame update
    void Start()
    {
        telegramInit();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    async void telegramInit()
    {
        _client = new TdClient();
        _client.Bindings.SetLogVerbosityLevel(0);

        _client.UpdateReceived += async (sender, update) =>
        {
            switch (update)
            {
                case TdApi.Update.UpdateOption option:
                    /*
                    await _client.ExecuteAsync(new TdApi.SetOption
                    {
                        DataType = option.DataType,
                        Extra = option.Extra,
                        Name = option.Name,
                        Value = option.Value
                    });
                    */
                    break;
                case TdApi.Update.UpdateAuthorizationState updateAuthorizationState when updateAuthorizationState.AuthorizationState.GetType() == typeof(TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters):
                    await _client.ExecuteAsync(new TdApi.SetTdlibParameters
                    {
                        Parameters = new TdApi.TdlibParameters
                        {
                            ApiId = ApiId,
                            ApiHash = ApiHash,
                            ApplicationVersion = "1.3.0",
                            DeviceModel = "PC",
                            SystemLanguageCode = "en",
                            SystemVersion = "Win 10.0"
                        }
                    });
                    break;
                case TdApi.Update.UpdateAuthorizationState updateAuthorizationState when updateAuthorizationState.AuthorizationState.GetType() == typeof(TdApi.AuthorizationState.AuthorizationStateWaitEncryptionKey):
                    await _client.ExecuteAsync(new TdApi.CheckDatabaseEncryptionKey());
                    break;
                case TdApi.Update.UpdateAuthorizationState updateAuthorizationState when updateAuthorizationState.AuthorizationState.GetType() == typeof(TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber):
                    _authNeeded = true;
                    ResetEvent.Set();
                    break;
                case TdApi.Update.UpdateAuthorizationState updateAuthorizationState when updateAuthorizationState.AuthorizationState.GetType() == typeof(TdApi.AuthorizationState.AuthorizationStateWaitCode):
                    _authNeeded = true;
                    ResetEvent.Set();
                    break;
                case TdApi.Update.UpdateUser updateUser:
                    ResetEvent.Set();
                    break;
                case TdApi.Update.UpdateConnectionState updateConnectionState when updateConnectionState.State.GetType() == typeof(TdApi.ConnectionState.ConnectionStateReady):
                    break;

                default:
                    ; // add a breakpoint here to see other events
                    break;
            }
        };

        Debug.Log("[taeyeon] ResetEvent.Wait " );
        ResetEvent.Wait();
        if (_authNeeded)
        {
            await _client.ExecuteAsync(new TdApi.SetAuthenticationPhoneNumber
            {
                PhoneNumber = PhoneNumber
            });
//            Console.Write("Insert the login code: ");
            Debug.Log("[taeyeon] Insert the login code : ");
            //            var code = Console.ReadLine();
            string code = "91527";
            await _client.ExecuteAsync(new TdApi.CheckAuthenticationCode
            {
                Code = code
            });
        }

        GetChannels();
/*
        await foreach (var chat in GetChannels())
        {
            Console.WriteLine(chat.Title);
        }
*/
        Console.ReadLine();
    }

    //    public static async IAsyncEnumerable<TdApi.Chat> GetChannels(int limit = 1000)
    public static async void GetChannels(int limit = 1000)
//    public static async TdApi.Chat[] GetChannels(int limit = 1000)
    {
        var chats = await _client.ExecuteAsync(new TdApi.GetChats { Limit = limit });
        foreach (var chatId in chats.ChatIds)
        {
            var chat = await _client.ExecuteAsync(new TdApi.GetChat { ChatId = chatId });
            if (chat.Type is TdApi.ChatType.ChatTypeSupergroup || chat.Type is TdApi.ChatType.ChatTypeBasicGroup || chat.Type is TdApi.ChatType.ChatTypePrivate)
            {
                //                yield return chat;
                Debug.Log("[taeyeon] GetChannels chat.Title : " + chat.Title);
                Debug.Log("[taeyeon] GetChannels chat.Id : " + chat.Id);
                //Console.WriteLine(chat.Title);
            }
        }

        long from_message_id = 0;
        int left = 0;

        while (left > -10)
        {
            var m_Messages = await _client.ExecuteAsync(new TdApi.GetChatHistory { ChatId = 5091764862, FromMessageId = from_message_id, Offset = left, Limit = 10 });
            Debug.Log("[taeyeon] GetChannels m_Messages.TotalCount : " + m_Messages.TotalCount);
            if (m_Messages.TotalCount < 1)
            {
                // there is no more messages
                break;
            }

            foreach (var m_Message in m_Messages.Messages_)
            {

                Debug.Log("[taeyeon] GetChannels m_Message.CanGetViewers : " + m_Message.CanGetViewers);
                Debug.Log("[taeyeon] GetChannels m_Message.Id : " + m_Message.Id);
                from_message_id = m_Message.Id;
                Debug.Log("[taeyeon] GetChannels m_Message.Content.DataType : " + m_Message.Content.DataType);
                var m_MessageContent = (TdApi.MessageContent)m_Message.Content;
                var m_MessageText = (TdApi.MessageContent.MessageText)m_Message.Content;
                Debug.Log("[taeyeon] GetChannels m_MessageText.Text.Text : " + m_MessageText.Text.Text);
            }
            left = 0;
        }

    }

    public async Task Execute_WhenErrorExpected_ThrowsTdException()
    {
        using (var client = new TdClient())
        {
            TdException exception = null;

            try
            {
                await client.TestReturnErrorAsync(new TdApi.Error
                {
                    Code = 0,
                    Message = "Error"
                });
            }
            catch (TdException e)
            {
                exception = e;
            }
        }
    }
}
