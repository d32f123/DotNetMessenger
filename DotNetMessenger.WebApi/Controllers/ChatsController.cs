using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Logger;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Filters.Authorization;
using DotNetMessenger.WebApi.Filters.Logging;
using DotNetMessenger.WebApi.Models;
using DotNetMessenger.WebApi.Principals;
using NLog;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/chats")]
    [ExpectedExceptionsFilter]
    [TokenAuthentication]
    [Authorize]
    public class ChatsController : ApiController
    {
        private const string RegexString = @".*\/chats\/([^\/]+)\/?";

        /// <summary>
        /// Gets chat information by its id. User must be in chat
        /// </summary>
        /// <param name="id">The id of the chat</param>
        /// <returns>All chat information</returns>
        [Route("{id:int}")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = RegexString)]
        public Chat GetChatById(int id)
        {
            using (var timeLog =
                new ChronoLogger(LogLevel.Debug, "Fetching chat with id: {0}", id))
            {
                timeLog.Start();
                var chat = RepositoryBuilder.ChatsRepository.GetChat(id);
                NLogger.Logger.Info("Fetched chat with id {0}", id);
                return chat;
            }
        }
        /// <summary>
        /// Creates a new chat. List of users must include the sender
        /// </summary>
        /// <param name="chatCredentials">Chat title and members</param>
        /// <returns><see cref="Chat"/> object</returns>
        [Route("")]
        [HttpPost]
        public Chat CreateChat([FromBody] ChatCredentials chatCredentials)
        {
            NLogger.Logger.Debug("Called with arguments: {0}", chatCredentials);
            if (chatCredentials.Members == null)
            {
                NLogger.Logger.Error("{0} is null", nameof(chatCredentials));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No members"));
            }
            // check if current user is in chat
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("No principal set");
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server broken"));
            }
            var principal = (UserPrincipal) Thread.CurrentPrincipal;
            if (!chatCredentials.Members.Contains(principal.UserId))
            {
                NLogger.Logger.Error("{0} does not contain the person who is creating the chat",
                    nameof(chatCredentials.Members));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                    "Cannot create chat not including yourself"));
            }
            NLogger.Logger.Debug("Converting {0} to array", nameof(chatCredentials.Members));
            var members = chatCredentials.Members as int[] ?? chatCredentials.Members.ToArray();
            Chat chat;
            switch (chatCredentials.ChatType)
            {
                case ChatTypes.Dialog:
                    if (members.Length != 2)
                    {
                        NLogger.Logger.Error("Trying to create dialog but number of users != 2");
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                            "Dialog requires 2 users"));
                    }
                    using (var timeLog = new ChronoLogger("Creating dialog chat with parameters: {0}",
                        members))
                    {
                        timeLog.Start();
                        chat = RepositoryBuilder.ChatsRepository.CreateDialog(members[0], members[1]);
                        NLogger.Logger.Info("Dialog chat created. Id: {0}, Member1: {1}, Member2: {2}", chat.Id, members[0], members[1]);
                        return chat;
                    }
                case ChatTypes.GroupChat:
                {
                    using (var timeLog = new ChronoLogger("Creating group chat with parameters: {0}",
                        members))
                    {
                        timeLog.Start();
                        chat = RepositoryBuilder.ChatsRepository.CreateGroupChat(members, chatCredentials.Title);
                        NLogger.Logger.Info("Group chat created. Id: {0}, Members: {1}", chat.Id, members);
                        return chat;
                    }
                }
                default:
                    NLogger.Logger.Error("Unknown chat type: {0}", chatCredentials.ChatType);
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "Invalid chat type"));
            }
        }
        /// <summary>
        /// Creates or fetches an existing dialog between the two users
        /// </summary>
        /// <param name="user1">User 1</param>
        /// <param name="user2">User 2</param>
        /// <returns>A chat between the two users</returns>
        [Route("dialog/{user1:int}/{user2:int}")]
        [HttpGet]
        public Chat CreateOrGetDialogChat(int user1, int user2)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", user1, user2);
            var chat = RepositoryBuilder.ChatsRepository.CreateOrGetDialog(user1, user2);
            NLogger.Logger.Info("Fetched dialog between {0} and {1}. Id: {2}", user1, user2, chat.Id);
            return chat;
        }
        /// <summary>
        /// Deletes chat. User must be the creator of the chat
        /// </summary>
        /// <param name="id">Chat's id</param>
        [Route("{id:int}")]
        [HttpDelete]
        [UserIsChatCreatorAuthorization(RegexString = RegexString)]
        public void DeleteChat(int id)
        {
            NLogger.Logger.Debug("Called with arguments: {0}", id);
            using (var timeLog = new ChronoLogger("Deleting chat with id: {0}", id))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.DeleteChat(id);
                NLogger.Logger.Info("Deleted chat with id {0}", id);
            }
        }
        /// <summary>
        /// Gets chat information (title et c.). User must be in chat
        /// </summary>
        /// <param name="id">The id of the chat</param>
        /// <returns>Title, avatar et c.</returns>
        [Route("{id:int}/info")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = RegexString)]
        public ChatInfo GetChatInfo(int id)
        {
            NLogger.Logger.Debug("Called with arguments: {0}", id);
            using (var timeLog = new ChronoLogger("Fetching chat info for id: {0}", id))
            {
                timeLog.Start();
                var chatInfo = RepositoryBuilder.ChatsRepository.GetChatInfo(id);
                NLogger.Logger.Info("Fetched chat info for chat {0}", id);
                return chatInfo;
            }
        }
        /// <summary>
        /// Deletes chat information. User must have <see cref="RolePermissions.ChatInfoPerm"/>
        /// </summary>
        /// <param name="id">The id of the chat</param>
        [Route("{id:int}/info")]
        [HttpDelete]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ChatInfoPerm)]
        public void DeleteChatInfo(int id)
        {
            NLogger.Logger.Debug("Called with arguments: {0}", id);
            using (var timeLog = new ChronoLogger("Deleting chat info for id: {0}", id))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.DeleteChatInfo(id);
                NLogger.Logger.Info("Deleted chat info for id: {0}", id);
            }
        }
        /// <summary>
        /// Set's information for the chat. User must have <see cref="RolePermissions.ChatInfoPerm"/> permissions
        /// </summary>
        /// <param name="id">The id of the chat</param>
        /// <param name="chatInfo">Information to be set</param>
        [Route("{id:int}/info")]
        [HttpPut]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ChatInfoPerm)]
        public void SetChatInfo(int id, [FromBody] ChatInfo chatInfo)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", id, chatInfo);
            using (var timeLog = new ChronoLogger("Setting chat info for id {0}. Info: {1}",
                id, chatInfo))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetChatInfo(id, chatInfo);
                NLogger.Logger.Info("Successfuly set chat info for id: {0}. Info: {1}", id, chatInfo);
            }
        }
        /// <summary>
        /// Gets a chat's members. User performing the request must be in chat
        /// </summary>
        /// <param name="id">The id of the chat</param>
        /// <returns>Chat's members</returns>
        [Route("{id:int}/users")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = RegexString)]
        public List<User> GetChatUsers(int id)
        {
            NLogger.Logger.Debug("Called with arguments: {0}", id);
            using (var timeLog = new ChronoLogger("Fetching chat users for id: {0}", id))
            {
                timeLog.Start();
                var users = RepositoryBuilder.ChatsRepository.GetChatUsers(id) as List<User> ??
                            RepositoryBuilder.ChatsRepository.GetChatUsers(id).ToList();
                users.ForEach(x =>
                {
                    if (x.ChatUserInfos != null)
                    {
                        if (x.ChatUserInfos.ContainsKey(id))
                        {
                            var value = x.ChatUserInfos[id];
                            x.ChatUserInfos.Clear();
                            x.ChatUserInfos.Add(id, value);
                        }
                        else
                        {
                            x.ChatUserInfos = null;
                        }
                    }
                    if (x.Chats != null)
                    {
                        x.Chats = x.Chats.Where(y => y.Id == id).ToList();
                    }
                });
                NLogger.Logger.Info("Successfully fetched chat users of chat: {0}", id);
                return users;
            }
        }
        /// <summary>
        /// Adds a user to the chat. User performing the request must have <see cref="RolePermissions.ManageUsersPerm"/> permission
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the new member</param>
        [Route("{chatId:int}/users/{userId:int}")]
        [HttpPost]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ManageUsersPerm)]
        public void AddUser(int chatId, int userId)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", chatId, userId);
            using (var timeLog = new ChronoLogger("Adding user {0} to chat {1}", userId, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.AddUser(chatId, userId);
                NLogger.Logger.Info("Successfully added user {0} to chat {1}", userId, chatId);
            }
        }
        /// <summary>
        /// Kicks a user from the chat. User performing the request must have <see cref="RolePermissions.ManageUsersPerm"/> permission
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the kicked member</param>
        [Route("{chatId:int}/users/{userId:int}")]
        [HttpDelete]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ManageUsersPerm)]
        public void KickUser(int chatId, int userId)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", chatId, userId);
            using (var timeLog = new ChronoLogger("Kicking user {0} from chat {1}", userId, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.KickUser(chatId, userId);
                NLogger.Logger.Info("Successfully kicked user {0} from chat {1}", userId, chatId);
            }
        }
        /// <summary>
        /// Adds a list of users to a given chat. User making the request must have <see cref="RolePermissions.ManageUsersPerm"/> permission
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userIds">List of users to be added</param>
        [Route("{chatId:int}/users")]
        [HttpPost]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ManageUsersPerm)]
        public void AddUsers(int chatId, [FromBody] IEnumerable<int> userIds)
        {
            var newUsers = userIds as int[] ?? userIds.ToArray();
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", chatId, newUsers);
            using (var timeLog =
                new ChronoLogger("Adding users {0} to chat {1}", userIds, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.AddUsers(chatId, newUsers);
                NLogger.Logger.Info("Successfully added users to chat {0}. Users: {1}", chatId, newUsers);
            }
        }
        /// <summary>
        /// Kicks a list of users from a given chat. User making the request must have <see cref="RolePermissions.ManageUsersPerm"/> permission
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userIds">List of users to be kicked</param>
        [Route("{chatId:int}/users")]
        [HttpDelete]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ManageUsersPerm)]
        public void KickUsers(int chatId, [FromBody] IEnumerable<int> userIds)
        {
            var kickedUsers = userIds as int[] ?? userIds.ToArray();
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", chatId, kickedUsers);
            using (var timeLog =
                new ChronoLogger("Kicking users {0} from chat {1}", userIds, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.KickUsers(chatId, kickedUsers);
                NLogger.Logger.Info("Successfully kicked users from chat {0}. Users: {1}", chatId, kickedUsers);
            }
        }
        /// <summary>
        /// Sets a new creator for the chat. User performing the request must be the current creator 
        /// and the new creator must already be in chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the user that will be the new creator</param>
        [Route("{chatId:int}/users/creator/{userId:int}")]
        [HttpPut]
        [UserIsChatCreatorAuthorization(RegexString = RegexString)]
        public void SetCreator(int chatId, int userId)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", chatId, userId);
            using (var timeLog =
                new ChronoLogger("Setting creator for chat {1}. UserID: {0}", userId, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetCreator(chatId, userId);
                NLogger.Logger.Info("Successfully set new creator for chat {0}. New creator: {1}", chatId, userId);
            }
        }
        /// <summary>
        /// Gets nickname and user role of a given user in a given chat. User performing the request must be in chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the user</param>
        /// <returns>Information about the user specific to the given chat</returns>
        [Route("{chatId:int}/users/{userId:int}/info")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = RegexString)]
        public ChatUserInfo GetChatSpecificUserInfo(int chatId, int userId)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", chatId, userId);
            using (var timeLog =
                new ChronoLogger("Fetching chat-specific user info for userId: {0} in chatId: {1}",
                    userId, chatId))
            {
                timeLog.Start();
                var chatUserInfo = RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(userId, chatId);
                NLogger.Logger.Info("Successfully fetched chat-specific user info for user {0} in chat {1}",
                    userId, chatId);
                return chatUserInfo;
            }
        }
        /// <summary>
        /// Deletes user info for the specified user. User performing the request must have <see cref="RolePermissions.ManageUsersPerm"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the user</param>
        [Route("{chatId:int}/users/{userId:int}/info")]
        [HttpDelete]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ManageUsersPerm)]
        public void DeleteChatSpecificUserInfo(int chatId, int userId)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", chatId, userId);
            using (var timeLog =
                new ChronoLogger("Deleting chat-specific user info for userId: {0} in chatId: {1}",
                    userId, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.ClearChatSpecificInfo(userId, chatId);
                NLogger.Logger.Info("Successfully deleted chat-specific user info for user {0} in chat {1}",
                    userId, chatId);
            }
        }
        /// <summary>
        /// Sets user info for specified user. User performing the request must have <see cref="RolePermissions.ManageUsersPerm"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="chatUserInfo">New information for the user</param>
        [Route("{chatId:int}/users/{userId:int}/info")]
        [HttpPut]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ManageUsersPerm)]
        public void SetChatSpecificUserInfo(int chatId, int userId, [FromBody] ChatUserInfo chatUserInfo)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}, {2}", chatId, userId, chatUserInfo);
            using (var timeLog =
                new ChronoLogger("Setting chat-specific user info for userId: {0}, in chatId: {1}, info: {2}",
                    userId, chatId, chatUserInfo))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(userId, chatId, chatUserInfo,
                    chatUserInfo.Role != null);
                NLogger.Logger.Info("Successfully set chat-specific user info for user {0} in chat {1}. Info: {2}",
                    userId, chatId, chatUserInfo);
            }
        }
        /// <summary>
        /// Sets user role for specified user. User performing the request must have <see cref="RolePermissions.ManageUsersPerm"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleId">New role id</param>
        /// <returns>Updated user information</returns>
        [Route("{chatId:int}/users/{userId:int}/info/role/{roleId:int}")]
        [HttpPut]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ManageUsersPerm)]
        public ChatUserInfo SetChatSpecificUserRole(int chatId, int userId, int roleId)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}, {2}", chatId, userId, roleId);
            using (var timeLog =
                new ChronoLogger("Setting chat-specific user role for userId: {0}, in chatId: {1}, role: {2}",
                    userId, chatId, roleId))
            {
                timeLog.Start();
                var chatUserInfo = RepositoryBuilder.ChatsRepository.SetChatSpecificRole(userId, chatId, (UserRoles) roleId);
                NLogger.Logger.Info("Successfully set chat-specific user role for user {0} in chat {1}. Role: {2}",
                    userId, chatId, roleId);
                return chatUserInfo;
            }
        }
        /// <summary>
        /// Sets given user information (title and avatar) for the specified chat (does not set role at any circumstance).
        /// User performing the request must be in chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="chatUserInfo">New information</param>
        [Route("{chatId:int}/users/current/info")]
        [HttpPut]
        [ChatUserAuthorization(RegexString = RegexString)]
        public void SetChatUserInfoForCurrentUser(int chatId, [FromBody] ChatUserInfo chatUserInfo)
        {
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", chatId, chatUserInfo);
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("No principal set");
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server broken"));
            }
            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            using (var timeLog =
                new ChronoLogger("Setting chat-specific user info for caller, chatId: {0}, info: {1}", chatId, chatUserInfo))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(principal.UserId, chatId, chatUserInfo);
                NLogger.Logger.Info("Successfully set chat-specific user info for caller in chat {0}. Info: {1}",
                    chatId, chatUserInfo);
            }
        }
        /// <summary>
        /// Clears nickname for the current user. User performing the request must be in chat
        /// </summary>
        /// <param name="chatId">The id of the user</param>
        [Route("{chatId:int}/users/current/info")]
        [HttpDelete]
        [ChatUserAuthorization(RegexString = RegexString)]
        public void ClearChatUserInfoForCurrentUser(int chatId)
        {
            NLogger.Logger.Debug("Called with arguments: {0}", chatId);
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("No principal set");
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server broken"));
            }
            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            using (var timeLog =
                new ChronoLogger("Clearing chat-specific user info for caller, chatId: {0}", chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(principal.UserId, chatId, new ChatUserInfo
                {
                    Nickname = null,
                    Role = null
                });
                NLogger.Logger.Info("Successfully cleared chat-specific user info for caller in chat {0}", chatId);
            }
        }
        /// <summary>
        /// Checks for new messages in selected chats. New messages are considered to have ID higher than the provided
        /// </summary>
        /// <param name="chatMessagePairs">A list of pairs (chat, lastMessage)</param>
        /// <returns>List of new messages for the client</returns>
        [Route("messages/subscribe")]
        [HttpPut]
        public List<Message> GetNewMessages([FromBody] IEnumerable<Message> chatMessagePairs)
        {
            NLogger.Logger.Debug("Called");
            if (!(Thread.CurrentPrincipal is UserPrincipal principal))
            {
                NLogger.Logger.Warn("Could not get user principal");
                return null;
            }

            var currChat = -1;
            var msgs = chatMessagePairs as List<Message> ?? chatMessagePairs.ToList();
            var userId = principal.UserId;
            foreach (var msg in msgs)
            {
                if (msg.ChatId == currChat) continue;
                // check for perms
                currChat = msg.ChatId;
                if (!RepositoryBuilder.ChatsRepository.CheckForChatUser(userId, currChat))
                    throw new ArgumentException();
            }
            return RepositoryBuilder.MessagesRepository.GetChatsMessagesFrom(msgs).ToList();
        }
        /// <summary>
        /// Creates a subscription for new chats
        /// </summary>
        /// <param name="chatId">The id of the last chat the client has</param>
        /// <returns>A list of new chats for the client</returns>
        [Route("subscribe/{chatId:int}")]
        [HttpGet]
        public List<Chat> GetNewChats(int chatId)
        {
            NLogger.Logger.Debug("Called with arguments: {0}", chatId);
            NLogger.Logger.Debug("Fetching current user");

            if (!(Thread.CurrentPrincipal is UserPrincipal principal))
            {
                NLogger.Logger.Warn("Could not get user principal");
                return null;
            }

            var userId = principal.UserId;

            NLogger.Logger.Debug("Returning latest chats");
            return RepositoryBuilder.ChatsRepository.GetUserChats(userId)
                .Where(x => x.Id > chatId && x.ChatType == ChatTypes.GroupChat).ToList();
        }
    }
}
