using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ParseData
{
    public class ParseGrouponSelenium
    {
        public ParseGrouponSelenium()
        {
            var driver = new ChromeDriver(@"..\..\..\");

            driver.Navigate().GoToUrl("https://www.hvr.co.il/signin.aspx");

            Thread.Sleep(2000);

            var idnumber = driver.FindElementById("tz");
            var password = driver.FindElementById("password");
            var submitButton = driver.FindElementById("sgLoginButton").FindElement(By.TagName("a"));

            idnumber.SendKeys("308334721");
            password.SendKeys("135246");
            submitButton.Click();

            Thread.Sleep(2000);

            var heverteamimButton = driver.FindElementsByCssSelector(".sidebar_ul li")[2];
            heverteamimButton.Click();
            var v = driver.FindElementsByCssSelector("#branch-table tbody tr");
        }
    }
}
