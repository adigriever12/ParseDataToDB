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

            driver.Navigate().GoToUrl("https://www.groupon.co.il/browse/tel-aviv-iw?category=food-and-drink");

            Thread.Sleep(1000);

            //var v = driver.FindElementByCssSelector("ul li.refinement ul.children-list")
        }
    }
}
