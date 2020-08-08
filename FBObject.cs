using System;
using System.Collections.Generic;

namespace MyStupidBots
{
    class From
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    class CommentData
    {
        public DateTime created_time { get; set; }
        public From from { get; set; }
        public string message { get; set; }
        public string id { get; set; }
    }

    class ReactData
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    class Cursors
    {
        public string before { get; set; }
        public string after { get; set; }
    }

    class Paging
    {
        public Cursors cursors { get; set; }
        public string next { get; set; }
    }

    class FBComment
    {
        public List<CommentData> data { get; set; }
        public Paging paging { get; set; }
    }

    class FBReact
    {
        public List<ReactData> data { get; set; }
        public Paging paging { get; set; }
    }
}