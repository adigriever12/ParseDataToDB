using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ParseData.DataSet1TableAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ParseData
{
    public class ParseAmerican
    {
        public ParseAmerican()
        {
            DataSet1.LocationsDataTable locationsDataTable = new DataSet1.LocationsDataTable();
            //DataSet1.Laumi_RestuarantDataTable laumiRrestuarantsDataTable = new DataSet1.Laumi_RestuarantDataTable();

            LocationsTableAdapter locationsAdapter = new LocationsTableAdapter();
            //Laumi_RestuarantTableAdapter laumiRestuarantsAdapter = new Laumi_RestuarantTableAdapter();

            locationsAdapter.Fill(locationsDataTable);

            WebClient client = new WebClient();

            var driver = new ChromeDriver(@"..\..\..\");

            driver.Navigate().GoToUrl("https://rewards.americanexpress.co.il/rewards/restaurants-cafes/?id=383");

            var restaurants = driver.FindElementsByCssSelector(".Float.SearchSimpleImage a").Select(x => x.GetAttribute("href")).ToList();

            foreach (string currRes in restaurants)
            {
                driver.Navigate().GoToUrl(currRes);

                if (driver.FindElementById("ctl00_MainPlaceHolder_lblTitle1").Text.Contains("בורגר"))
                {
                    continue;
                }

                var deleteSpan = driver.FindElementByCssSelector("#ctl00_MainPlaceHolder_ctlBenefitInformation_second p").FindElements(By.TagName("span")).Last().Text;

                var strongsSpans = driver.FindElementByCssSelector("#ctl00_MainPlaceHolder_ctlBenefitInformation_second p").FindElements(By.XPath("*")).Where(x => x.Text != deleteSpan);

                string description = string.Join("\n", strongsSpans.Select(x => x.Text));


                var p = driver.FindElementsByCssSelector(".iParagraphContent.Float p").First(x => x.Text.Contains("איך נהנים מההטבה"));  //x => x.FindElements(By.TagName("span")).Select(y => y.Text == "איך נהנים מההטבה").Count() > 0);
                deleteSpan = p.FindElements(By.TagName("span")).Last().Text;
                strongsSpans = p.FindElements(By.XPath("*")).Where(x => x.Text != deleteSpan);

                string perks = string.Join("\n", strongsSpans.Select(x => x.Text));


                var addressSpan = driver.FindElementsByCssSelector(".iParagraphContent.Float p span").First(x => x.Text.Contains("כתובת"));

                var splits = addressSpan.GetAttribute("innerHTML").Split(new[] { "<br><br>" }, StringSplitOptions.RemoveEmptyEntries);

                // NOT finished yet
                var check = "";
            }
        }
    }
}
