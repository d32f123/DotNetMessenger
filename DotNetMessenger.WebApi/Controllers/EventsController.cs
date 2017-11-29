using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.Logger;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Filters.Logging;
using DotNetMessenger.WebApi.Models;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/events")]
    [ExpectedExceptionsFilter]
    [TokenAuthentication]
    [Authorize]
    public class EventsController : ApiController
    {
        [HttpPost]
        public ServerInfoOutput GetNewInfo([FromBody] ClientState clientState)
        {
            NLogger.Logger.Debug("Called");

            NLogger.Logger.Debug("Fetching current user");

            if (!(Thread.CurrentPrincipal is UserPrincipal principal))
            {
                NLogger.Logger.Warn("Could not get user principal");
                return null;
            }

            var userId = principal.UserId;
            // TODO: CHECK FOR PERMS
            var output = new ServerInfoOutput();
            // get all new users that the client is not aware of
            output.NewUsers = RepositoryBuilder.UsersRepository.GetUsersInRange(clientState.LastUserId + 1, int.MaxValue);

            var outputNewUsers = output.NewUsers as User[] ?? output.NewUsers.ToArray();
            NLogger.Logger.Debug("Fetched new users, total: {0}", outputNewUsers.Length);
            // create a dictionary: [userId, userHash]
            var clientHashes = clientState.UsersStates.ToDictionary(clientUser => clientUser.UserId, clientUser => clientUser.UserHash);
            // get users from db
            var dbUsers =
                RepositoryBuilder.UsersRepository.GetUsersInList(clientState.UsersStates.Select(x => x.UserId)
                    .Concat(outputNewUsers.Select(x => x.Id))).ToList();

            // return to client all users whose hashes are different from what the client gave us
            output.UsersWithNewInfo = dbUsers.Where(x => !clientHashes.ContainsKey(x.Id) ||
                                                         x.GetHashCode() != clientHashes[x.Id]).ToList();

            NLogger.Logger.Debug("Fetched users with new info, total: {0}",
                output.UsersWithNewInfo.Any() ? output.UsersWithNewInfo.Count() : 0);

            // get all the new chats that the client is not aware of
            output.NewChats = RepositoryBuilder.ChatsRepository.GetUserChats(userId)
                .Where(x => x.Id > clientState.LastChatId && x.ChatType == ChatTypes.GroupChat).ToList();
            NLogger.Logger.Debug("Fetched new chats, total: {0}", output.NewChats.Count());
            

            // get a list of messages that the client does not have
            var newMessages = RepositoryBuilder.MessagesRepository.GetChatsMessagesFrom(
                clientState.ChatsStates.Select(x => new Message {Id = x.LastChatMessageId, ChatId = x.ChatId}));
#if DEBUG
            NLogger.Logger.Debug("Fetched new messages, total: {0}", newMessages.Count());
#endif
            var newMessagesDictionary = GetDictionaryFromMessageList(newMessages);

            output.NewChatInfo = new List<ChatInfoOutput>();
            foreach (var chatState in clientState.ChatsStates)
            {
                var shouldCommit = false;
                var returnState = new ChatInfoOutput();

                returnState.ChatId = chatState.ChatId;

                // if client's chatInfo hash is not the same as ours, give them the new chatInfo
                var isDialog = false;
                var chat = RepositoryBuilder.ChatsRepository.GetChat(chatState.ChatId);

                isDialog = chat.ChatType == ChatTypes.Dialog;

                if (chat.GetHashCode() != chatState.ChatHash)
                {
                    shouldCommit = true;
                    returnState.Chat = chat;
                    NLogger.Logger.Debug("Chat {0} has changed for user", returnState.ChatId);
                }
                else
                {
                    returnState.Chat = null;
                }

                // if client's latest message is not the same as ours, give them the new messages
                if (newMessagesDictionary.ContainsKey(chatState.ChatId))
                {
                    shouldCommit = true;
                    returnState.NewChatMessages = newMessagesDictionary[chatState.ChatId];
                    NLogger.Logger.Debug("Chat {0} has new messages", returnState.ChatId);
                }
                else
                {
                    returnState.NewChatMessages = null;
                }

                // if client does not have some of the members, return them as well
                if (!isDialog)
                {
                    var newMembers =
                        RepositoryBuilder.ChatsRepository.GetNotListedChatMembers(chatState.ChatId,
                            chatState.CurrentMembers.Select(x => x.UserId)).ToList();
                    if (newMembers.Any())
                    {
                        shouldCommit = true;
                        returnState.NewMembers = newMembers;
                        NLogger.Logger.Debug("Chat {0} has new members", returnState.ChatId);
                    }
                    else
                    {
                        returnState.NewMembers = null;
                    }
                }
                else
                {
                    returnState.NewMembers = null;
                }

                if (!isDialog)
                {
                    var dbInfos = RepositoryBuilder.ChatsRepository.GetChatMembersInfo(chatState.ChatId);
                    var clientInfo = chatState.CurrentMembers.ToDictionary(x => x.UserId, x => x.UserHash);
                    var newClientInfo = new Dictionary<int, ChatUserInfo>();
                    foreach (var dbInfo in dbInfos)
                    {
                        if (!clientInfo.ContainsKey(dbInfo.Key))
                        {
                            newClientInfo.Add(dbInfo.Key, dbInfo.Value);
                            NLogger.Logger.Debug("User {0} info in chat {1} has changed", dbInfo.Key, chatState.ChatId);
                        }
                        else if (clientInfo[dbInfo.Key] != -1 && clientInfo[dbInfo.Key] != dbInfo.Value.GetHashCode())
                        {
                            newClientInfo.Add(dbInfo.Key, dbInfo.Value);
                            NLogger.Logger.Debug("User {0} info in chat {1} has changed", dbInfo.Key, chatState.ChatId);
                        }
                    }

                    if (newClientInfo.Count != 0)
                    {
                        shouldCommit = true;
                        returnState.NewChatUserInfos = newClientInfo;
                    }
                    else
                    {
                        returnState.NewChatUserInfos = null;
                    }
                }
                else
                {
                    returnState.NewChatUserInfos = null;
                }

                if (shouldCommit)
                    ((List<ChatInfoOutput>) output.NewChatInfo).Add(returnState);
            }

            return output;
        }

        private Dictionary<int, IEnumerable<Message>> GetDictionaryFromMessageList(IEnumerable<Message> messages)
        {
            var ret = new Dictionary<int, IEnumerable<Message>>();

            var lastChatId = -1;
            var chatMessages = new List<Message>();
            foreach (var msg in messages)
            {
                if (msg.ChatId != lastChatId)
                {
                    if (lastChatId != -1)
                        ret.Add(lastChatId, chatMessages);
                    lastChatId = msg.ChatId;
                    chatMessages = new List<Message>();
                }
                chatMessages.Add(msg);
            }
            if (lastChatId != -1)
                ret.Add(lastChatId, chatMessages);

            return ret;
        }
    }
}
