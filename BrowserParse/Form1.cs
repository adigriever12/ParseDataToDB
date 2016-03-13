using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BrowserParse
{
    public partial class Form1 : Form
    {
        private string downloaded;
        private byte[] bytes;
        public Form1()
        {
            InitializeComponent();
            //WebClient c = new WebClient();
            //var values = new NameValueCollection
            //{
            //    { "tz", "308334721" },
            //    { "password", "135246" },
            //    { "oMode", "login" },
            //    { "tmpl_filename", "signin"},
            //    {"redirect", "http://www.hvr.co.il/home_page.aspx?page=hvr_home"},
            //    { "cn", "2759252239"}
            //};

            //c.UploadValues("https://www.hvr.co.il/signin.aspx", values);
            //downloaded = c.DownloadString("http://www.hvr.co.il/staticPage.aspx?page=teamim_info.html");
            webBrowser1.ObjectForScripting = new MyScript();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            string formParams = string.Format("tz={0}&password={1}&oMode={2}&tmpl_filename={3}&redirect={4}&cn={5}",
                "308334721", "135246", "login", "signin", "http://www.hvr.co.il/home_page.aspx?page=hvr_home", "2759252239");

            bytes = Encoding.ASCII.GetBytes(formParams);
            //req.ContentLength = bytes.Length;
            //using (Stream os = req.GetRequestStream())
            //{
            //    os.Write(bytes, 0, bytes.Length);
            //}
            //WebResponse resp = req.GetResponse();
            //cookieHeader = resp.Headers["Set-cookie"];


            webBrowser1.Navigate("https://www.hvr.co.il/signin.aspx", "", bytes, "");
            webBrowser1.Navigate("http://www.hvr.co.il/staticPage.aspx?page=teamim_info.html");
            //webBrowser1.DocumentText = downloaded;
            //Navigate("http://www.hvr.co.il/staticPage.aspx?page=teamim_info.html");
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webBrowser1.Navigate("javascript: window.external.CallServerSideCode();", "", bytes, "");
        }

    }

    [ComVisible(true)]
    public class MyScript
    {
        public void CallServerSideCode()
        {
            var doc = ((Form1)Application.OpenForms[0]).webBrowser1.Document;

            var tbody = doc.GetElementById("branch-table").Children[1];

            foreach (HtmlElement htmlElement in tbody.Children)
            {
                
            }
        }
    }


}
