using System.Collections.Generic;

using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.DataLayer
{
    /// <summary>
    /// Interface for interacting with user-related entities in DB
    /// </summary>
    public interface IChatsRepository
    {
        /// <summary>
        /// Creates a group chat from a given list and with a given title
        /// </summary>
        /// <param name="members">List of members of the to-be-created chat</param>
        /// <param name="title">The title of the new chat</param>
        /// <returns>Null on invalid ids, else object representing a newly created chat</returns>
        /// <remarks>The first member of the <paramref name="members"/> is the creator of the chat</remarks>
        /// <remarks>ID 0 is not allowed</remarks>
        Chat CreateGroupChat(IEnumerable<int> members, string title);
        /// <summary>
        /// Creates a dialog chat with 2 users
        /// </summary>
        /// <param name="member1">The first user of the dialog</param>
        /// <param name="member2">The second user of the dialog</param>
        /// <returns>Null on invalid ids, else object representing a newly created chat</returns>
        /// <remarks>ID 0 is not allowed</remarks>
        Chat CreateDialog(int member1, int member2);
        /// <summary>
        /// Returns a chat entity given chat id
        /// </summary>
        /// <param name="chatId">Id of the chat to be returned</param>
        /// <returns>Null if invalid members, else chat with a given Id</returns>
        Chat GetChat(int chatId);
        /// <summary>
        /// Gets a dialog between two users
        /// </summary>
        /// <param name="member1">User 1</param>
        /// <param name="member2">User 2</param>
        /// <returns>Dialog between the two users</returns>
        Chat CreateOrGetDialog(int member1, int member2);
        /// <summary>
        /// Gets a list of users in a chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>Empty list on invalid id, else list of users in a given chat</returns>
        IEnumerable<User> GetChatUsers(int chatId);
        /// <summary>
        /// Gets a list of chats for a specific user
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>A list of chats that the user is in</returns>
        /// <remarks>ID 0 is not allowed</remarks>
        IEnumerable<Chat> GetUserChats(int userId);
        /// <summary>
        /// Deletes a chat given its id
        /// </summary>
        /// <param name="chatId"></param>
        void DeleteChat(int chatId);
        /// <summary>
        /// Sets a new creator for a given chat
        /// <paramref name="newCreator"/> should be already in chat and != 0
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="newCreator"></param>
        void SetCreator(int chatId, int newCreator);
        /// <summary>
        /// Adds a new user to a given chat
        /// </summary>
        /// <param name="chatId">The id of the affected chat</param>
        /// <param name="userId">The id of the user</param>
        void AddUser(int chatId, int userId);
        /// <summary>
        /// Kicks a user from the given chat
        /// </summary>
        /// <param name="chatId">The id of the affected chat</param>
        /// <param name="userId">The id of the user</param>
        void KickUser(int chatId, int userId);
        /// <summary>
        /// Adds a list of users into a given chat
        /// </summary>
        /// <param name="chatId">The id of the affected chat</param>
        /// <param name="newUsers">List of the to-be-inserted user ids</param>
        void AddUsers(int chatId, IEnumerable<int> newUsers);
        /// <summary>
        /// Kicks a list of users from a given chat
        /// </summary>
        /// <param name="chatId">The id of the affected chat</param>
        /// <param name="kickedUsers">List of the to-be-kicked user ids</param>
        void KickUsers(int chatId, IEnumerable<int> kickedUsers);
        /// <summary>
        /// Sets title and other information for the given chat
        /// </summary>
        /// <param name="chatId">id of the chat</param>
        /// <param name="info">Title and other information for the chat</param>
        void SetChatInfo(int chatId, ChatInfo info);
        /// <summary>
        /// Gets title and other information about the given chat
        /// </summary>
        /// <param name="chatId">Id of the chat</param>
        /// <returns>Null on invalidId, else information relating the given chat</returns>
        ChatInfo GetChatInfo(int chatId);
        /// <summary>
        /// Deletes information regarding a given chat
        /// </summary>
        /// <param name="chatId">The chat id</param>
        void DeleteChatInfo(int chatId);

        // Chat-specific user info
        /// <summary>
        /// Gets information for a given <paramref name="userId"/> specific to a given <paramref name="chatId"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="chatId"></param>
        /// <returns>Null if no info exists or invalid ids, else information regarding user for a given chat</returns>
        ChatUserInfo GetChatSpecificInfo(int userId, int chatId);
        /// <summary>
        /// Sets <paramref name="userInfo"/> for a given <paramref name="userId"/> specific to a given <paramref name="chatId"/>
        /// </summary>
        /// <param name="userId">The id of the user(user should be in chat)</param>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userInfo">The information regarding a user</param>
        /// <param name="updateRole">Indicates whether to update <see cref="UserRole"/></param>
        void SetChatSpecificInfo(int userId, int chatId, ChatUserInfo userInfo, bool updateRole = false);
        /// <summary>
        /// Sets <see cref="UserRole"/> for a given <paramref name="userId"/> and <paramref name="chatId"/>
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="chatId">The id of the new chat</param>
        /// <param name="userRole">The new role</param>
        /// <returns>Null if invalid id, else info regarding the user</returns>
        ChatUserInfo SetChatSpecificRole(int userId, int chatId, UserRoles userRole);
        /// <summary>
        /// Gets the user role given its id
        /// </summary>
        /// <returns>Object representing the user role</returns>
        UserRole GetUserRole(UserRoles roleId);
        /// <summary>
        /// Deletes information regarding a specific user and a chat
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="chatId">The id of the chat</param>
        void ClearChatSpecificInfo(int userId, int chatId);
    }
}
