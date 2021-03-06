﻿using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.WebApi.Handlers;

namespace DotNetMessenger.WebApi
{
    public static class WebApiConfig
    {
        private static string ConnectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";

        public static void Register(HttpConfiguration config)
        {
            // Конфигурация и службы веб-API
            RepositoryBuilder.ConnectionString = ConnectionString;
            // Маршруты веб-API
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.MessageHandlers.Add(new LoggingHandler());
        }
    }
}
