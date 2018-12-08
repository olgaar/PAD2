using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using MongoDB.Driver.Linq;
using HttpServer;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace HTTPServer
{
    // Класс-обработчик клиента
    class Client
    {
        // Отправка страницы с ошибкой
        
        public StringBuilder BuildAllMooviesPage(List<Moovie> moovies)
        {
            var builder = new StringBuilder();
            builder.Append("<html><body><h1>It works!</h1><ul>");
            foreach (var moovie in moovies)
            {
                builder.AppendFormat("<li>Id: {0}, Name: {1}, Year: {2}, Genre: {3}</li>",
                moovie.Id, moovie.Name, moovie.Year, moovie.Genre);
            }
            builder.Append(@"</ul></body></html>");
            return builder;
        }


        public StringBuilder BuildPostPage()

        {
            var builder = new StringBuilder();
            builder.Append("<html><body><h1>It works!</h1><form method=\"post\"> Add new moovie to DB <br />Name:  <input type=\"text\" name=\"name\" /><br />Year:   <input type=\"text\" name=\"year\" /><br />Genre: <input type=\"text\" name=\"genre\" /><br /><input type=\"submit\" value=\"Submit\" /></form></body></html>");

            return builder;
        }
        public StringBuilder BuildSingleMooviePage(Moovie moovie)
        {
            var builder = new StringBuilder();
            builder.Append("<html><body><h1>It works!</h1><ul>");
            
                builder.AppendFormat("<li>Id: {0}, Name: {1}, Year: {2}, Genre: {3}</li>",
                moovie.Id, moovie.Name, moovie.Year, moovie.Genre);
            
            builder.Append(@"</ul></body></html>");
            return builder;
        }
        public static string GetRequestPostData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return null;
            }
            using (System.IO.Stream body = request.InputStream) // here we have data
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public IMongoCollection<Moovie> GetMongoCollection()
        {
            const string connectionString = "mongodb://127.0.0.1:27017";

            // Create a MongoClient object by using the connection string
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("moovies");

            //get mongodb collection
            var collection = database.GetCollection<Moovie>("moovies");
            return collection;
        }

        // Конструктор класса. Ему нужно передавать принятого клиента от HttpListener
        public Client(HttpListenerContext context)
        {
            
            var builder = new StringBuilder();
            Console.WriteLine($"{context.Request.HttpMethod}: {context.Request.Url}");
            Match ReqMatch = Regex.Match(context.Request.Url.PathAndQuery, @"movies/+");
            if (context.Request.Url.PathAndQuery.EndsWith("/movies"))
            {
                var collection = GetMongoCollection();
                var movies = collection.AsQueryable<Moovie>().ToList();
                builder = BuildAllMooviesPage(movies);
            }
            else if (!String.IsNullOrEmpty(ReqMatch.Value)){
                var collection = GetMongoCollection();
                var movies = collection.AsQueryable<Moovie>().ToList();
                builder = BuildAllMooviesPage(movies);
                var url = context.Request.Url.ToString();
                var movieId = Regex.Split(url, "/movies/")[1];
                var movie = movies.Find(x => x.Id.ToString().Contains(movieId));
                if (movie!=null) builder = BuildSingleMooviePage(movie);
            }
            else if (context.Request.Url.PathAndQuery.EndsWith("/addMovie"))
            {
                builder = BuildPostPage();
            }
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.HttpMethod == "POST")
            {
                string data = GetRequestPostData(request);
                var collection = GetMongoCollection();
                var name = data.Split('=', '&')[1];
                var year = data.Split('=', '&')[3];
                var genre = data.Split('=', '&')[5];
                collection.InsertOneAsync(new Moovie { Name = name, Year = Int32.Parse(year), Genre=genre });
                

            }

            byte[] buffer = Encoding.UTF8.GetBytes(builder.ToString());
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
    }

    class Server
    {
        HttpListener Listener; // Объект, принимающий клиентов

        // Запуск сервера
        public Server(int Port)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://127.0.0.1:8080/");
            Listener.Start();
            Console.WriteLine("Ожидание подключений...");
            
            // В бесконечном цикле
            while (true)
            {
                var context = Listener.GetContext();
                ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), context);
            }
        }

        static void ClientThread(Object StateInfo)
        {
            // Просто создаем новый экземпляр класса Client и передаем ему приведенный к классу TcpClient объект StateInfo
            new Client((HttpListenerContext)StateInfo);
        }

        // Остановка сервера
        ~Server()
        {
            // Если "слушатель" был создан
            if (Listener != null)
            {
                // Остановим его
                Listener.Stop();
            }
        }

        static void Main(string[] args)
        {
            // Определим нужное максимальное количество потоков
            // Пусть будет по 4 на каждый процессор
            int MaxThreadsCount = Environment.ProcessorCount * 4;
            // Установим максимальное количество рабочих потоков
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            // Установим минимальное количество рабочих потоков
            ThreadPool.SetMinThreads(2, 2);
            // Создадим новый сервер на порту 80
            new Server(8080);
        }
    }
    //}
}
