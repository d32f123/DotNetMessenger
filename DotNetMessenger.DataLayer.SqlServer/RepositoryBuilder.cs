namespace DotNetMessenger.DataLayer.SqlServer
{
    public static class RepositoryBuilder
    {
        private static string _connectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";

        public static string ConnectionString
        {
            get => _connectionString;
            set
            {
                _chatsRepository = null;
                _usersRepository = null;
                _messagesRepository = null;
                _tokensRepository = null;
                
                _connectionString = value;
            }
        }

        private static ChatsRepository _chatsRepository;
        private static UsersRepository _usersRepository;
        private static MessagesRepository _messagesRepository;
        private static TokensRepository _tokensRepository;

        public static ChatsRepository ChatsRepository
        {
            get
            {
                if (_chatsRepository != null) return _chatsRepository;
                if (_usersRepository == null)
                    _usersRepository = new UsersRepository(_connectionString);
                _chatsRepository = new ChatsRepository(_connectionString, _usersRepository);
                return _chatsRepository;
            }
        }

        public static UsersRepository UsersRepository
        {
            get
            {
                if (_usersRepository != null) return _usersRepository;
                if (_chatsRepository == null)
                    _chatsRepository = new ChatsRepository(_connectionString);
                _usersRepository = new UsersRepository(_connectionString);
                _chatsRepository.UsersRepository = _usersRepository;
                return _usersRepository;
            }
        }

        public static MessagesRepository MessagesRepository =>
            _messagesRepository ?? (_messagesRepository = new MessagesRepository(_connectionString));

        public static TokensRepository TokensRepository =>
            _tokensRepository ?? (_tokensRepository = new TokensRepository(_connectionString));

    }
}