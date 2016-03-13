using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ParseData
{
    public class ParseHeverTeamim
    {
        private HtmlDocument _doc;

        public ParseHeverTeamim()
        {
            string Url = "http://www.metacritic.com/game/pc/halo-spartan-assault";
            HtmlWeb web = new HtmlWeb();


            WebClient c = new WebClient();
            var values = new NameValueCollection
            {
                { "tz", "308334721" },
                { "password", "135246" },
                { "oMode", "login" },
                { "tmpl_filename", "signin"},
                {"redirect", "http://www.hvr.co.il/home_page.aspx?page=hvr_home"},
                { "cn", "2759252239"}
            };

            c.UploadValues("https://www.hvr.co.il/signin.aspx", values);

            string downloaded = c.DownloadString("http://www.hvr.co.il/staticPage.aspx?page=teamim_info.html");

            HtmlDocument d = new HtmlDocument();
            d.LoadHtml(downloaded);

            var collection = d.DocumentNode.SelectNodes("//table[@id='branch-table']/tbody")[0].ChildNodes;

            foreach (HtmlNode currentNode in collection)
            {
                var tt = currentNode;
                Console.WriteLine();
            }
            //_doc = web.Load(Url);


            //WebBrowser
        }

        public void Parse()
        {
            
        }
    }
}
