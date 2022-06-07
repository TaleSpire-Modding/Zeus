using System;
using System.Collections.Generic;

namespace Zeus
{
    public class Version
    {
        public string name { get; set; }
        public string full_name { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string version_number { get; set; }
        public List<string> dependencies { get; set; }
        public string download_url { get; set; }
        public int downloads { get; set; }
        public DateTime date_created { get; set; }
        public string website_url { get; set; }
        public bool is_active { get; set; }
        public string uuid4 { get; set; }
        public int file_size { get; set; }
    }
}
