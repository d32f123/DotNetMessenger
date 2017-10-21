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
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Filters.Authorization;
using DotNetMessenger.WebApi.Models;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/users")]
    [TokenAuthentication]
    [Authorize]
    public class UsersController : ApiController
    {
        /// <summary>
        /// Gets information about the specified user.
        /// If user performing the request is not the same as <paramref name="id"/> then chats and chatuserinfos fields are null
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <returns>Information about the user</returns>
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

            // if the user performing the request is getting information about other user
            // then we should not give out confidential info
            user.ChatUserInfos = null;
            user.Chats = null;
            return user;
        }
        /// <summary>
        /// Gets information about the specified user by username. If user performing the request is not the same as <paramref name="username"/>
        /// then chats and chatuserinfos are set to null
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <returns>Information about the user</returns>
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
        /// <summary>
        /// Gets list of chats the user has. User performing the request must be the same as <paramref name="id"/>
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <returns>Chats that the user is in</returns>
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
        /// <summary>
        /// Gets the list of chats the user is in. User performing tre request must be the same as <paramref name="username"/>
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <returns>List of chats the user is in</returns>
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
        /// <summary>
        /// Creates a new user from <paramref name="userCredentials"/>. Does not require authentication.
        /// </summary>
        /// <param name="userCredentials">The credentials of the new user</param>
        /// <returns><see cref="User"/> object representing the new user</returns>
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
        /// <summary>
        /// Deletes user given their <paramref name="id"/>. User performing the request must be the same as <paramref name="id"/>
        /// </summary>
        /// <param name="id">Id of the user to be deleted</param>
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
        /// <summary>
        /// Gets information about the user. 
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <returns>Information abot the user (name, avatar et c.)</returns>
        [Route("{id:int}/userinfo")]
        [HttpGet]
        public UserInfo GetUserInfo(int id)
        {
            var userInfo = RepositoryBuilder.UsersRepository.GetUserInfo(id);
            if (userInfo == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            return userInfo;
        }
        /// <summary>
        /// Deletes information about the specified user. User that is performing the request must be
        /// the same as <paramref name="id"/>
        /// </summary>
        /// <param name="id">The id of the user</param>
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
        /// <summary>
        /// Sets information about the specified user. User that is performing the request must be the same as <paramref name="id"/>
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <param name="userInfo">New information</param>
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
        /// <summary>
        /// Updates user info (including username) about the user. User performing the request must be
        /// the same as <paramref name="id"/>
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <param name="user">New information about the user</param>
        /// <returns>Updated information about the user</returns>
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
        /// <summary>
        /// Sets a new password for the user. User performing the request mustbe the same as <paramref name="id"/>
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <param name="newPassword">New password of the user</param>
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
