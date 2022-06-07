using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus
{
    public class Package
    {
        public string name { get; set; }
        public string full_name { get; set; }
        public string owner { get; set; }
        public string package_url { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_updated { get; set; }
        public string uuid4 { get; set; }
        public int rating_score { get; set; }
        public bool is_pinned { get; set; }
        public bool is_deprecated { get; set; }
        public bool has_nsfw_content { get; set; }
        public List<string> categories { get; set; }
        public List<Version> versions { get; set; }
        public string donation_link { get; set; }
    }
}
