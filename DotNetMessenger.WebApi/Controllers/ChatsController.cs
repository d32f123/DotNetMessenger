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
    }
}
