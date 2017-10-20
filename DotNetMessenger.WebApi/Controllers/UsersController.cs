using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using DotNetMessenger.Model;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.WebApi.Filters;
using DotNetMessenger.WebApi.Models;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        [Route("{id:int}")]
        [HttpGet]
        [TokenAuthentication]
        public User GetUserById(int id)
        {
            var user = RepositoryBuilder.UsersRepository.GetUser(id);
            if (user == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "No user found"));
            return user;
        }

        [Route("{username:length(6,50)}")]
        [HttpGet]
        public User GetUserByUsername(string username)
        {
            var user = RepositoryBuilder.UsersRepository.GetUserByUsername(username);
            if (user == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "No user found"));
            return user;
        }

        [Route("{id:int}/chats")]
        [HttpGet]
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

        [Route("")]
        [HttpPut]
        public User PersistUser([FromBody] User user)
        {
            try
            {
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
