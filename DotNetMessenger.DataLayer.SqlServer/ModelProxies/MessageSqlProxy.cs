using System;
using System.Collections.Generic;
using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer.SqlServer.ModelProxies
{
    public class MessageSqlProxy : Message
    {
        private bool _areAttachmentsFetched;
        private IEnumerable<Attachment> _attachments;

        private bool _isExpirationDateFetched;
        private DateTime? _expirationDate;


        public override DateTime? ExpirationDate
        {
            get
            {
                if (_isExpirationDateFetched)
                    return _expirationDate;
                _expirationDate = RepositoryBuilder.MessagesRepository.GetMessageExpirationDate(Id);
                _isExpirationDateFetched = true;
                return _expirationDate;
            }
        }

        public override IEnumerable<Attachment> Attachments
        {
            get
            {
                if (_areAttachmentsFetched)
                    return _attachments;

                _attachments = RepositoryBuilder.MessagesRepository.GetMessageAttachments(Id);
                _areAttachmentsFetched = true;
                return _attachments;
            }
        }
    }
}