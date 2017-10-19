using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Model;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/messages")]
    public class MessagesController : ApiController
    {
        [Route("{chatId:int}/{userId:int}")]
        [HttpPost]
        public Message StoreMessage(int chatId, int userId, [FromBody] Message message)
        {
            try
            {
                return RepositoryBuilder.MessagesRepository.StoreMessage(userId, chatId, message.Text,
                    message.Attachments);
            }
        }
    }
}
