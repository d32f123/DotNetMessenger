using System;

namespace DotNetMessenger.DataLayer.SqlServer.Exceptions
{
    public class ChatTypeMismatchException : Exception
    {
        public ChatTypeMismatchException()
        {
        }

        public ChatTypeMismatchException(string message)
            : base(message)
        {
        }

        public ChatTypeMismatchException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class UserIsCreatorException : Exception
    {
        public UserIsCreatorException()
        {
        }

        public UserIsCreatorException(string message)
            : base(message)
        {
        }

        public UserIsCreatorException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}