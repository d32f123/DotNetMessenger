using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;

using DotNetMessenger.Model;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.WebApi.Filters;
using DotNetMessenger.WebApi.Models;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/users")]
    [TokenAuthentication]
    [Authorize]
    public class UsersController : ApiController
    {
        [Route("{id:int}")]
        [HttpGet]
        public User GetUserById(int id)
        {
            var user = RepositoryBuilder.UsersRepository.GetUser(id);
            if (user == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "No user found"));

            if (!(Thread.CurrentPrincipal is UserPrincipal))
                return user;
            var principal = (UserPrincipal) Thread.CurrentPrincipal;

            if (principal.UserId == id) return user;

            user.ChatUserInfos = null;
            user.Chats = null;
            return user;
        }

        [Route("{username:length(6,50)}")]
        [HttpGet]
        public User GetUserByUsername(string username)
        {
            var user = RepositoryBuilder.UsersRepository.GetUserByUsername(username);
            if (user == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "No user found"));

            if (!(Thread.CurrentPrincipal is UserPrincipal))
                return user;
            var principal = (UserPrincipal)Thread.CurrentPrincipal;

            if (principal.UserId == user.Id) return user;

            user.ChatUserInfos = null;
            user.Chats = null;
            return user;
        }

        [Route("{id:int}/chats")]
        [HttpGet]
        [UserIdIsIdFromUriAuthorization(RegexString = @".*\/users\/([^\/]+)\/?")]
        public IEnumerable<Chat> GetUserChats(int id)
        {
            var chats = RepositoryBuilder.ChatsRepository.GetUserChats(id);
            try
            {
                var userChats = chats as Chat[] ?? chats.ToArray();
                return userChats;
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "No user found"));
            }
        }

        [Route("{username:length(6,50)}/chats")]
        [HttpGet]
        [UserNameIsNameFromUriAuthorization(RegexString = @".*\/users\/([^\/]+)\/?")]
        public IEnumerable<Chat> GetUserChatsByUsername(string username)
        {
            var user = RepositoryBuilder.UsersRepository.GetUserByUsername(username);
            if (user == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "No user found"));
            return RepositoryBuilder.ChatsRepository.GetUserChats(user.Id);
        }

        [Route("")]
        [HttpPost]
        [AllowAnonymous]
        public User CreateUser([FromBody] UserCredentials userCredentials)
        {
            try
            {
                return RepositoryBuilder.UsersRepository.CreateUser(userCredentials.Username, userCredentials.Password);
            }
            catch (UserAlreadyExistsException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "User already exists"));
            }
        }

        [Route("{id:int}")]
        [HttpDelete]
        [UserIdIsIdFromUriAuthorization(RegexString = @".*\/users\/([^\/]+)\/?")]
        public void DeleteUser(int id)
        {
            try
            {
                RepositoryBuilder.UsersRepository.DeleteUser(id);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }

        [Route("{id:int}/userinfo")]
        [HttpGet]
        public UserInfo GetUserInfo(int id)
        {
            var userInfo = RepositoryBuilder.UsersRepository.GetUserInfo(id);
            if (userInfo == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            return userInfo;
        }

        [Route("{id:int}/userinfo")]
        [HttpDelete]
        [UserIdIsIdFromUriAuthorization(RegexString = @".*\/users\/([^\/]+)\/?")]
        public void DeleteUserInfo(int id)
        {
            try
            {
                RepositoryBuilder.UsersRepository.DeleteUserInfo(id);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }

        [Route("{id:int}/userinfo")]
        [HttpPut]
        [UserIdIsIdFromUriAuthorization(RegexString = @".*\/users\/([^\/]+)\/?")]
        public void SetUserInfo(int id, [FromBody] UserInfo userInfo)
        {
            try
            {
                RepositoryBuilder.UsersRepository.SetUserInfo(id, userInfo);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "No userInfo provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }

        [Route("{id:int}")]
        [HttpPut]
        [UserIdIsIdFromUriAuthorization(RegexString = @".*\/users\/([^\/]+)\/?")]
        public User PersistUser(int id, [FromBody] User user)
        {
            try
            {
                user.Id = id;
                return RepositoryBuilder.UsersRepository.PersistUser(user);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable,
                    "No user provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable,
                    "Invalid id"));
            }
            catch (UserAlreadyExistsException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "User already exists"));
            }
        }

        [Route("{id:int}/password")]
        [HttpPut]
        [UserIdIsIdFromUriAuthorization(RegexString = @".*\/users\/([^\/]+)\/?")]
        public void SetPassword(int id, [FromBody] string newPassword)
        {
            try
            {
                RepositoryBuilder.UsersRepository.SetPassword(id, newPassword);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable,
                    "No password provided"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable,
                    "Invalid id"));
            }
        }
    }
}
