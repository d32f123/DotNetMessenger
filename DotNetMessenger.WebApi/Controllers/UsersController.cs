using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http;

using DotNetMessenger.Model;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Logger;
using DotNetMessenger.WebApi.Events;
using DotNetMessenger.WebApi.Extensions;
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Filters.Authorization;
using DotNetMessenger.WebApi.Filters.Logging;
using DotNetMessenger.WebApi.Models;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/users")]
    [ExpectedExceptionsFilter]
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
            NLogger.Logger.Debug("Called with argument UID:{0}", id);
            var user = RepositoryBuilder.UsersRepository.GetUser(id);
            NLogger.Logger.Debug("Fetched user with id {0}", id);
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Warn("Could not get user principal");
                return user;
            }
            var principal = (UserPrincipal) Thread.CurrentPrincipal;

            if (principal.UserId == id)
            {
                NLogger.Logger.Info("Fetched user with id {0}. The user is the caller", id);
                return user;
            }

            // if the user performing the request is getting information about other user
            // then we should not give out confidential info
            user.ChatUserInfos = null;
            user.Chats = null;
            NLogger.Logger.Info("Fetched user with id {0}. THe user is not the caller", id);
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
            NLogger.Logger.Debug("Called with argument userName:\"{0}\"", username);
            var user = RepositoryBuilder.UsersRepository.GetUserByUsername(username);
            NLogger.Logger.Debug("Fetched user with username {0}", username);

            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Warn("Could not get user principal");
                user.ChatUserInfos = null;
                user.Chats = null;
                return user;
            }
            var principal = (UserPrincipal)Thread.CurrentPrincipal;

            if (principal.UserId == user.Id)
            {
                NLogger.Logger.Info("Fetched user with username {0}. The user is the caller", username);
                return user;
            }

            user.ChatUserInfos = null;
            user.Chats = null;
            NLogger.Logger.Info("Fetched user with username {0}. The user is not the caller", username);
            return user;
        }
        /// <summary>
        /// Gets a list of all the users in DB
        /// </summary>
        /// <returns>List of all the users</returns>
        [Route("")]
        [HttpGet]
        public IEnumerable<User> GetAllUsers()
        {
            NLogger.Logger.Debug("Called");
            using (var timeLogger = new ChronoLogger("{0}: Fetching all users", nameof(GetAllUsers)))
            {
                timeLogger.Start();
                var users = RepositoryBuilder.UsersRepository.GetAllUsers();
                var usersList = users as List<User> ?? users.ToList();
                usersList.ForEach(x =>
                {
                    x.ChatUserInfos = null;
                    x.Chats = null;
                });
                NLogger.Logger.Info("All users successfully fetched. Total fetched: {0}", usersList.Count);
                return usersList;
            }
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
            NLogger.Logger.Debug("Called with argument UID:{0}", id);

            using (var timeLogger = new ChronoLogger("Fetching chats of UID:{0}", id))
            {
                timeLogger.Start();
                var chats = RepositoryBuilder.ChatsRepository.GetUserChats(id);
                var userChats = chats as Chat[] ?? chats.ToArray();
                NLogger.Logger.Info("Chats of user {0} successfully fetched. Total fetched: {1}",
                    id, userChats.Length);
                return userChats;
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
            NLogger.Logger.Debug("Called with argument username:\"{0}\"", username);
            using (var timeLogger = new ChronoLogger("{0}: Fetching chats of user \"{1\"",
                nameof(GetUserChatsByUsername), username))
            {
                timeLogger.Start();
                var user = RepositoryBuilder.UsersRepository.GetUserByUsername(username);
                var chats = RepositoryBuilder.ChatsRepository.GetUserChats(user.Id);
                var userChats = chats as Chat[] ?? chats.ToArray();
                NLogger.Logger.Info("Chats of user \"{0}\" successfully fetched. Total fetched: {1}",
                    username, userChats.Length);
                return userChats;
            }
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
            NLogger.Logger.Debug("Called with argument \"{0}\"", userCredentials.Username);
            var hash = PasswordHasher.PasswordToHash(userCredentials.Password);
            var user =  RepositoryBuilder.UsersRepository.CreateUser(userCredentials.Username, hash);
            NLogger.Logger.Info("Successfully created user with username \"{0}\"", userCredentials.Username);
            return user;
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
            NLogger.Logger.Debug("Called with argument {0}", id);
            using (var timeLogger = new ChronoLogger("Deleting user with UID: {0}", id))
            {
                timeLogger.Start();
                RepositoryBuilder.UsersRepository.DeleteUser(id);
                NLogger.Logger.Info("Successfully deleted user UID: {0}", id);
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
            NLogger.Logger.Debug("Called with argument {0}", id);
            var userInfo = RepositoryBuilder.UsersRepository.GetUserInfo(id);
            NLogger.Logger.Info("Successfully fetched user info for UID: {0}", id);
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
            NLogger.Logger.Debug("Called with argument {0}", id);
            RepositoryBuilder.UsersRepository.DeleteUserInfo(id);
            NLogger.Logger.Info("Successfully fetched user info for UID: {0}", id);
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
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", id, userInfo);
            RepositoryBuilder.UsersRepository.SetUserInfo(id, userInfo);
            NLogger.Logger.Info("Successfully set user info for UID: {0}. Info: {1}", id, userInfo);
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
            NLogger.Logger.Debug("Called with arguments: {0}, {1}", id, user);
            using (var timeLogger = new ChronoLogger("Persisting user with id {0}", id))
            {
                timeLogger.Start();
                user.Id = id;
                var newUser = RepositoryBuilder.UsersRepository.PersistUser(user);
                NLogger.Logger.Info("Successfully persisted user with id: {0}", id);
                return newUser;
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
            NLogger.Logger.Debug("Called with arguments: {0}", id);
            var hash = PasswordHasher.PasswordToHash(newPassword);
            RepositoryBuilder.UsersRepository.SetPassword(id, hash);
            NLogger.Logger.Info("Successfully set password for UID: {0}", id);
        }
        [Route("subscribe/{lastUserId:int}")]
        [HttpGet]
        public List<User> SubscribeForNewChats(int lastUserId)
        {
            User lastUser;
            NLogger.Logger.Debug("Called with arguments: {0}", lastUserId);
            try
            {
                NLogger.Logger.Debug("Fetching current chat");
                lastUser = lastUserId >= 0
                    ? RepositoryBuilder.UsersRepository.GetUser(lastUserId)
                    : null;
            }
            catch
            {
                NLogger.Logger.Debug("Invalid ID provided. Will subscribe to any new chat");
                lastUser = null;
            }

            // check for already existing new chats
            if (lastUser != null)
            {
                var users = RepositoryBuilder.UsersRepository.GetAllUsers().ToList();
                if (users.Any(x => x.Id > lastUserId))
                    return users.Where(x => x.Id > lastUserId).ToList();
            }
            NLogger.Logger.Debug("Creating a poller to subscribe for new users");
            var poller = new SinglePoller(new NewUserSubscription(), -1);
            while (!poller.SubscriptionInvoked)
                Thread.Sleep(500);
            NLogger.Logger.Debug("New user registered. Returning user to subscriber");
            return lastUser == null
                ? RepositoryBuilder.UsersRepository.GetAllUsers().ToList()
                : RepositoryBuilder.UsersRepository.GetAllUsers().Where(x => x.Id > lastUserId).ToList();
        }
    }
}
