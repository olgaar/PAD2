using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    public class Moovie
    {
        public ObjectId Id { get; set; }
        public String Name { get; set; }
        public int Year { get; set; }
        public String Genre { get; set; }
    }
}
