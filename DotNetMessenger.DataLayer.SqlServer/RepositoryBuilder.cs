namespace DotNetMessenger.DataLayer.SqlServer
{
    public static class RepositoryBuilder
    {
        private static string _connectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";

        public static string ConnectionString
        {
            get
            {
                return _connectionString;
            }
            set
            {
                _chatsRepository = null;
                _usersRepository = null;
                _messagesRepository = null;
                
                _connectionString = value;
            }
        }

        private static ChatsRepository _chatsRepository;
        private static UsersRepository _usersRepository;
        private static MessagesRepository _messagesRepository;

        public static ChatsRepository ChatsRepository
        {
            get
            {
                if (_chatsRepository == null)
                {
                    if (_usersRepository == null)
                        _usersRepository = new UsersRepository(_connectionString);
                    _chatsRepository = new ChatsRepository(_connectionString, _usersRepository);
                    _usersRepository.ChatsRepository = _chatsRepository;
                }
                return _chatsRepository;
            }
        }

        public static UsersRepository UsersRepository
        {
            get
            {
                if (_usersRepository == null)
                {
                    if (_chatsRepository == null)
                        _chatsRepository = new ChatsRepository(_connectionString);
                    _usersRepository = new UsersRepository(_connectionString, _chatsRepository);
                    _chatsRepository.UsersRepository = _usersRepository;
                }
                return _usersRepository;
            }
        }

        public static MessagesRepository MessagesRepository
        {
            get { return _messagesRepository ?? (_messagesRepository = new MessagesRepository(_connectionString)); }
        }
    }
}