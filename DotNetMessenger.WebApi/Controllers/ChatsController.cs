using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Filters.Authorization;
using DotNetMessenger.WebApi.Models;
using DotNetMessenger.WebApi.Principals;

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
            var chat = RepositoryBuilder.ChatsRepository.GetChat(id);
            if (chat == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "No chat found"));
            return chat;
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
            if (chatCredentials.Members == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No members"));
            // check if current user is in chat
            if (!(Thread.CurrentPrincipal is UserPrincipal))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Server broken"));
            var principal = (UserPrincipal) Thread.CurrentPrincipal;
            if (!chatCredentials.Members.Contains(principal.UserId))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Cannot create chat not including yourself"));

            var members = chatCredentials.Members as int[] ?? chatCredentials.Members.ToArray();
            try
            {
                switch (chatCredentials.ChatType)
                {
                    case ChatTypes.Dialog:
                        if (members.Length != 2)
                            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                "Dialog requires 2 users"));
                        return RepositoryBuilder.ChatsRepository.CreateDialog(members[0], members[1]);
                    case ChatTypes.GroupChat:
                        return RepositoryBuilder.ChatsRepository.CreateGroupChat(members, chatCredentials.Title);
                    default:
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                            "Invalid chat type"));
                }
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Title cannot be empty"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "Some users are invalid"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.DeleteChat(id);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No such chat exists"));
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
            try
            {
                return RepositoryBuilder.ChatsRepository.GetChatInfo(id);
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No information for dialog chat"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No such chat exists"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.DeleteChatInfo(id);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No information exists"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No such chat exists"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.SetChatInfo(id, chatInfo);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No info provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat exists"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
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
            try
            {
                var users = RepositoryBuilder.ChatsRepository.GetChatUsers(id) as User[] ??
                            RepositoryBuilder.ChatsRepository.GetChatUsers(id).ToArray();
                return users;
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat exists"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.AddUser(chatId, userId);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat or user exists"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.KickUser(chatId, userId);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat or user exists"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
            }
            catch (UserIsCreatorException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden,
                    "Cannot kick creator"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.AddUsers(chatId, userIds);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No users provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat or user exists"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.KickUsers(chatId, userIds);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No users provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat or user exists"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
            }
            catch (UserIsCreatorException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden,
                    "Cannot kick creator"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.SetCreator(chatId, userId);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat or user exists or user is not in chat"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
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
            try
            {
                return RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(userId, chatId);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat or user exists or user is not in chat"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.DeleteChatSpecificInfo(userId, chatId);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No info found"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No such chat or user exists or user is not in chat"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
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
            try
            {
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(userId, chatId, chatUserInfo, chatUserInfo.Role != null);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No info provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No such chat or user exists or user is not in chat"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
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
            try
            {
                return RepositoryBuilder.ChatsRepository.SetChatSpecificRole(userId, chatId, (UserRoles) roleId);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No such chat or user exists or user is not in chat"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
            }
            catch (Exception)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden,
                    "Incorrect role"));
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
            if (!(Thread.CurrentPrincipal is UserPrincipal))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server broken"));
            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            try
            {
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(principal.UserId, chatId, chatUserInfo);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No info provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No such chat or user exists or user is not in chat"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
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
            if (!(Thread.CurrentPrincipal is UserPrincipal))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server broken"));
            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            try
            {
                RepositoryBuilder.ChatsRepository.SetChatSpecificInfo(principal.UserId, chatId, new ChatUserInfo
                {
                    Nickname = null, Role = null
                });
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No info provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "No such chat or user exists or user is not in chat"));
            }
            catch (ChatTypeMismatchException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Chat cannot be dialog"));
            }
        }
    }
}
