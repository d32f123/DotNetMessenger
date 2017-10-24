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
using DotNetMessenger.WebApi.Models;
using DotNetMessenger.WebApi.Principals;
using NLog;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/chats")]
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
                new ChronoLogger(LogLevel.Info, "{0}: Deleting chat with id: {1}", nameof(DeleteChat), id))
            {
                timeLog.Start();
                return RepositoryBuilder.ChatsRepository.GetChat(id);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}", nameof(CreateChat), chatCredentials);
            if (chatCredentials.Members == null)
            {
                NLogger.Logger.Error("{0}: {1} is null", nameof(CreateChat), nameof(chatCredentials));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No members"));
            }
            // check if current user is in chat
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("{0}: No principal set", nameof(CreateChat));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server broken"));
            }
            var principal = (UserPrincipal) Thread.CurrentPrincipal;
            if (!chatCredentials.Members.Contains(principal.UserId))
            {
                NLogger.Logger.Error("{0}: {1} does not contain the person who is creating the chat",
                    nameof(CreateChat), nameof(chatCredentials.Members));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                    "Cannot create chat not including yourself"));
            }
            NLogger.Logger.Debug("{0}: Converting {1} to array", nameof(CreateChat), nameof(chatCredentials.Members));
            var members = chatCredentials.Members as int[] ?? chatCredentials.Members.ToArray();
            switch (chatCredentials.ChatType)
            {
                case ChatTypes.Dialog:
                    if (members.Length != 2)
                    {
                        NLogger.Logger.Error("{0}: Trying to create dialog but number of users != 2", nameof(CreateChat));
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                            "Dialog requires 2 users"));
                    }
                    using (var timeLog = new ChronoLogger("{0}: Creating dialog chat with parameters: {1}",
                        nameof(CreateChat), members))
                    {
                        timeLog.Start();
                        return RepositoryBuilder.ChatsRepository.CreateDialog(members[0], members[1]);
                    }
                case ChatTypes.GroupChat:
                {
                    using (var timeLog = new ChronoLogger("{0}: Creating group chat with parameters: {1}",
                        nameof(CreateChat), members))
                    {
                            timeLog.Start();
                        return RepositoryBuilder.ChatsRepository.CreateGroupChat(members, chatCredentials.Title);
                    }
                }
                default:
                    NLogger.Logger.Error("{0}: Unknown chat type: {1}", nameof(CreateChat), chatCredentials.ChatType);
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "Invalid chat type"));
            }
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}", nameof(DeleteChat), id);
            using (var timeLog = new ChronoLogger("{0}: Deleting chat with id: {1}", nameof(DeleteChat), id))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.DeleteChat(id);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}", nameof(GetChatInfo), id);
            using (var timeLog = new ChronoLogger("{0}: Fetching chat info for id: {1}", nameof(GetChatInfo), id))
            {
                timeLog.Start();
                return RepositoryBuilder.ChatsRepository.GetChatInfo(id);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}", nameof(DeleteChatInfo), id);
            using (var timeLog = new ChronoLogger("{0}: Deleting chat info for id: {1}", nameof(DeleteChatInfo), id))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.DeleteChatInfo(id);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(SetChatInfo), id, chatInfo);
            using (var timeLog = new ChronoLogger("{0}: Setting chat info for id {1}. Info: {2}", nameof(SetChatInfo),
                id, chatInfo))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetChatInfo(id, chatInfo);
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
        public User[] GetChatUsers(int id)
        {
            NLogger.Logger.Debug("{0}: Called with arguments: {1}", nameof(GetChatUsers), id);
            using (var timeLog = new ChronoLogger("{0}: Fetching chat users for id: {1}", nameof(GetChatUsers), id))
            {
                timeLog.Start();
                var users = RepositoryBuilder.ChatsRepository.GetChatUsers(id) as User[] ??
                            RepositoryBuilder.ChatsRepository.GetChatUsers(id).ToArray();
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(AddUser), chatId, userId);
            using (var timeLog = new ChronoLogger("{0}: Adding user {1} to chat {2}", nameof(AddUser), userId, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.AddUser(chatId, userId);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(KickUser), chatId, userId);
            using (var timeLog = new ChronoLogger("{0}: Kicking user {1} from chat {2}", nameof(KickUser), userId, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.KickUser(chatId, userId);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(AddUsers), chatId, newUsers);
            using (var timeLog =
                new ChronoLogger("{0}: Adding users {1} to chat {2}", nameof(AddUsers), userIds, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.AddUsers(chatId, newUsers);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(KickUsers), chatId, kickedUsers);
            using (var timeLog =
                new ChronoLogger("{0}: Kicking users {1} from chat {2}", nameof(KickUsers), userIds, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.KickUsers(chatId, kickedUsers);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(SetCreator), chatId, userId);
            using (var timeLog =
                new ChronoLogger("{0}: Setting creator for chat {2}. UserID: {1}", nameof(SetCreator), userId, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetCreator(chatId, userId);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(GetChatSpecificUserInfo), chatId, userId);
            using (var timeLog =
                new ChronoLogger("{0}: Fetching chat-specific user info for userId: {1} in chatId: {2}",
                    nameof(GetChatSpecificUserInfo), userId, chatId))
            {
                timeLog.Start();
                return RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(userId, chatId);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(DeleteChatSpecificUserInfo), chatId, userId);
            using (var timeLog =
                new ChronoLogger("{0}: Deleting chat-specific user info for userId: {1} in chatId: {2}",
                    nameof(DeleteChatSpecificUserInfo), userId, chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.DeleteChatSpecificInfo(userId, chatId);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}, {3}", nameof(SetChatSpecificUserInfo), chatId,
                userId, chatUserInfo);
            using (var timeLog =
                new ChronoLogger("{0}: Setting chat-specific user info for userId: {1}, in chatId: {2}, info: {3}",
                    nameof(SetChatSpecificUserInfo), userId, chatId, chatUserInfo))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(userId, chatId, chatUserInfo,
                    chatUserInfo.Role != null);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}, {3}", nameof(SetChatSpecificUserRole), chatId,
                userId, roleId);
            using (var timeLog =
                new ChronoLogger("{0}: Setting chat-specific user role for userId: {1}, in chatId: {2}, role: {3}",
                    nameof(SetChatSpecificUserRole), userId, chatId, roleId))
            {
                timeLog.Start();
                return RepositoryBuilder.ChatsRepository.SetChatSpecificRole(userId, chatId, (UserRoles) roleId);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}, {2}", nameof(SetChatUserInfoForCurrentUser), chatId, chatUserInfo);
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("{0}: No principal set", nameof(SetChatUserInfoForCurrentUser));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server broken"));
            }
            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            using (var timeLog =
                new ChronoLogger("{0}: Setting chat-specific user info for caller, chatId: {1}, role: {2}",
                    nameof(SetChatUserInfoForCurrentUser), chatId, chatUserInfo))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(principal.UserId, chatId, chatUserInfo);
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
            NLogger.Logger.Debug("{0}: Called with arguments: {1}", nameof(ClearChatUserInfoForCurrentUser), chatId);
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("{0}: No principal set", nameof(ClearChatUserInfoForCurrentUser));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server broken"));
            }
            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            using (var timeLog =
                new ChronoLogger("{0}: Clearing chat-specific user info for caller, chatId: {1}",
                    nameof(SetChatUserInfoForCurrentUser), chatId))
            {
                timeLog.Start();
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(principal.UserId, chatId, new ChatUserInfo
                {
                    Nickname = null,
                    Role = null
                });
            }
        }
    }
}
