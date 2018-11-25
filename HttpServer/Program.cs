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
        private void SendError(TcpClient Client, int Code)
        {
            // Получаем строку вида "200 OK"
            // HttpStatusCode хранит в себе все статус-коды HTTP/1.1
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            // Код простой HTML-странички
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            // Необходимые заголовки: ответ сервера, тип и длина содержимого. После двух пустых строк - само содержимое
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            // Приведем строку к виду массива байт
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            // Отправим его клиенту
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            // Закроем соединение
            Client.Close();
        }
        public StringBuilder BuildAllMooviesPage(List<Moovie> moovies)
        {
            var builder = new StringBuilder();
            builder.Append("<html><body><h1>It works!</h1><ul>");
            foreach (var moovie in moovies)
            {
                builder.AppendFormat("<li>Id: {0}, Name: {1}, Year: {2}, Genre: {3}</li>",
                moovie.Id, moovie.Name, moovie.Year, moovie.Genre);
            }
            builder.Append(@"</ul><form><input type=""text"" size=""40""><input type=""submit"" value=""Submit""></form></body></html>");
            return builder;
        }


        public StringBuilder BuildPostPage()

        {
            var builder = new StringBuilder();
            //string text = File.ReadAllText(@"C:\Users\Olga\Desktop\index.html");
            //<form method=\"post\">First name: <input type=\"text\" name=\"firstname\" /><br />Last name: <input type=\"text\" name=\"lastname\" /><input type=\"submit\" value=\"Submit\" /></form>
            //builder.Append("<html><body><h1>It works!</h1><form method=\"post\">Mongo json: <input type=\"text\" name=\"mongoJson\" /><br /><input type=\"submit\" value=\"Submit\" /></form></body></html>");
            builder.Append("<html><body><h1>It works!</h1><form method=\"post\"> New moovie: <input type=\"text\" name=\"name\" /><br /><input type=\"text\" name=\"year\" /><br /><input type=\"text\" name=\"genre\" /><br /><input type=\"submit\" value=\"Submit\" /></form></body></html>");

            return builder;
        }
        public StringBuilder BuildSingleMooviePage(Moovie moovie)
        {
            var builder = new StringBuilder();
            builder.Append("<html><body><h1>It works!</h1><ul>");
            
                builder.AppendFormat("<li>Id: {0}, Name: {1}, Year: {2}, Genre: {3}</li>",
                moovie.Id, moovie.Name, moovie.Year, moovie.Genre);
            
            builder.Append(@"</ul><form><input type=""text"" size=""40""><input type=""submit"" value=""""></form></body></html>");
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
            const string connectionString = "mongodb://localhost:27017";

            // Create a MongoClient object by using the connection string
            var client = new MongoClient(connectionString);

            //Use the MongoClient to access the server
            var database = client.GetDatabase("moovies");

            //get mongodb collection
            var collection = database.GetCollection<Moovie>("moovies");
            //collection.InsertOneAsync(new Moovie { Name = "Jack" });
            return collection;
        }

        // Конструктор класса. Ему нужно передавать принятого клиента от TcpListener
        public Client(HttpListenerContext context)
        {
            
            var builder = new StringBuilder();
            Console.WriteLine($"{context.Request.HttpMethod}: {context.Request.Url}");
            Match ReqMatch = Regex.Match(context.Request.Url.PathAndQuery, @"mongo/+");
            if (context.Request.Url.PathAndQuery.EndsWith("/mongo"))
            {
                string connect = "mongodb://localhost:27017";
                var client = new MongoClient(connect);
                var mongoDatabase = client.GetDatabase("moovies");
                var collection = mongoDatabase.GetCollection<Moovie>("moovies").AsQueryable<Moovie>();
                var moovies = collection.ToList();
                
                builder = BuildAllMooviesPage(moovies);
            }
            else if (!String.IsNullOrEmpty(ReqMatch.Value)){
                string connect = "mongodb://localhost:27017";
                var client = new MongoClient(connect);
                var mongoDatabase = client.GetDatabase("moovies");
                var collection = mongoDatabase.GetCollection<Moovie>("moovies").AsQueryable<Moovie>();
                var moovies = collection.ToList();
                var url = context.Request.Url.ToString();
                var moovieId = Regex.Split(url, "/mongo/")[1];
                var moovie = moovies.Find(x => x.Id.ToString().Contains(moovieId));
                if (moovie!=null) builder = BuildSingleMooviePage(moovie);
            }
            else if (context.Request.Url.PathAndQuery.EndsWith("/post"))
            {
                const string connectionString = "mongodb://localhost:27017";

                // Create a MongoClient object by using the connection string
                var client = new MongoClient(connectionString);

                //Use the MongoClient to access the server
                var database = client.GetDatabase("moovies");

                //get mongodb collection
                var collection = database.GetCollection<Moovie>("moovies");
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
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
    }

    class Server
    {
        HttpListener Listener; // Объект, принимающий TCP-клиентов

        // Запуск сервера
        public Server(int Port)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://localhost:8080/");
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
