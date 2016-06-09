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
    public class ParseLaumi
    {
        public ParseLaumi()
        {
            DataSet1.LocationsDataTable locationsDataTable = new DataSet1.LocationsDataTable();
            DataSet1.Laumi_RestuarantDataTable laumiRrestuarantsDataTable = new DataSet1.Laumi_RestuarantDataTable();
            
            LocationsTableAdapter locationsAdapter = new LocationsTableAdapter();
            Laumi_RestuarantTableAdapter laumiRestuarantsAdapter = new Laumi_RestuarantTableAdapter();
            
            locationsAdapter.Fill(locationsDataTable);

            WebClient client = new WebClient();

            var driver = new ChromeDriver(@"..\..\..\");

            driver.Navigate().GoToUrl("https://www.leumi-card.co.il/he-il/Benefits/Pages/BenfitsGallery.aspx#club=&category=בתי קפה ומסעדות&region=");

            var restaurants = driver.FindElementsByCssSelector("#ctl00_WebPartManager_g_577e8a7a_7e1f_4ac0_902b_98c7c9a92554_ctl00_ulBenefitsResults li a").Select(x => x.GetAttribute("href")).ToList();

            var page = driver.FindElementByCssSelector(".genericClientSidePages li a");
            page.Click();
            var more = driver.FindElementsByCssSelector("#ctl00_WebPartManager_g_577e8a7a_7e1f_4ac0_902b_98c7c9a92554_ctl00_ulBenefitsResults li a").Select(x => x.GetAttribute("href"));
            restaurants.AddRange(more);

            var nextPage = driver.FindElementsByCssSelector(".genericClientSidePages li a").Skip(2).First();
            nextPage.Click();
            more = driver.FindElementsByCssSelector("#ctl00_WebPartManager_g_577e8a7a_7e1f_4ac0_902b_98c7c9a92554_ctl00_ulBenefitsResults li a").Select(x => x.GetAttribute("href"));
            restaurants.AddRange(more);


            foreach (string currRes in restaurants)
            {
                driver.Navigate().GoToUrl(currRes);

                // name
                string name = driver.FindElementByCssSelector(".benefitInfo_content h1").Text.Replace("לחץ כאן כדי להתאים את הדף לקורא מסך\r\n", "");

                // desctiption
                string description = driver.FindElementByClassName("richHtml").Text.Replace("\r\n\r\nלפירוט חבילות האירוח לחץ כאן", "");

                // perks
                var discounts = driver.FindElementsByCssSelector("#divVariantsTable tr[varianttype=SingleDiscount]");
                List<string> perksList = new List<string>();

                foreach (var currDis in discounts)
                {
                    var cols = currDis.FindElements(By.TagName("td"));

                    string perkName = cols[0].Text;
                    string price = cols[2].Text;
                    perksList.Add(perkName + " " + price);
                }

                string perks = string.Join("*" + Environment.NewLine, perksList);

                // location + phone
                var allDetails = driver.FindElementsByCssSelector("#whereToUse #branch_one");

                if (allDetails.Count == 0)
                {
                    continue;
                }

                var details = allDetails.Last();
                //var locationName = details.FindElement(By.ClassName("enterpriseName")).GetAttribute("innerHTML");
                var location = details.FindElements(By.TagName("p")).Last().Text;//.GetAttribute("innerHTML").Replace(locationName, "");
                var data = location.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                string phone = data.Last();
                var onlyLocation = data.Skip(1).Take(data.Count() - 2).ToList();

                // Image
                var logo = driver.FindElements(By.Id("benefitLogoImg"));

                string imageSrc;

                if (logo.Count > 0)
                {
                    imageSrc = logo.First().GetAttribute("src");
                }
                else
                {
                    imageSrc = driver.FindElement(By.Id("benefit_pic")).GetAttribute("src");
                }



                // DB
                // location
                DataSet1.LocationsRow addressRow = locationsDataTable.NewLocationsRow();
                addressRow.Address = String.Join(" ", onlyLocation);

                if (!ParseGrouponSelenium.ExtractGeoLocation(addressRow, onlyLocation))
                {
                    continue;
                }

                locationsDataTable.Rows.Add(addressRow);
                locationsAdapter.Update(locationsDataTable);

                DataSet1.Laumi_RestuarantRow resRow = laumiRrestuarantsDataTable.NewLaumi_RestuarantRow();

                resRow.Name = name;
                resRow.Location_LocationId = addressRow.LocationId;
                resRow.Description = description;
                resRow.Image = client.DownloadData(imageSrc);
                resRow.Phone = phone;
                resRow.Perks = perks;
                resRow.RankningUsersSum = 0;
                resRow.RankingsSum = 0;

                laumiRrestuarantsDataTable.Rows.Add(resRow);
                laumiRestuarantsAdapter.Update(laumiRrestuarantsDataTable);

            }
        }
        
    }
}
