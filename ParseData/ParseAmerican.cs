using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ParseData.DataSet1TableAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ParseData
{
    public class ParseAmerican
    {
        public ParseAmerican()
        {
            DataSet1.LocationsDataTable locationsDataTable = new DataSet1.LocationsDataTable();
            DataSet1.American_RestuarantDataTable americanRrestuarantsDataTable = new DataSet1.American_RestuarantDataTable();

            LocationsTableAdapter locationsAdapter = new LocationsTableAdapter();
            American_RestuarantTableAdapter amercanAdapter = new American_RestuarantTableAdapter();

            locationsAdapter.Fill(locationsDataTable);

            WebClient client = new WebClient();

            var driver = new ChromeDriver(@"..\..\..\");

            driver.Navigate().GoToUrl("https://rewards.americanexpress.co.il/rewards/restaurants-cafes/?id=383");

            var restaurants = driver.FindElementsByCssSelector(".Float.SearchSimpleImage a").Select(x => x.GetAttribute("href")).ToList();

            foreach (string currRes in restaurants)
            {
                driver.Navigate().GoToUrl(currRes);


                string checkName = driver.FindElementById("ctl00_MainPlaceHolder_lblTitle1").Text;
                if (checkName.Contains("בורגר") || checkName.Contains("פיצה האט") || checkName.Contains("שוק העיר"))
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


                //var addressSpan = driver.FindElementsByCssSelector(".iParagraphContent.Float p span").First(x => x.Text.Contains("כתובת"));

                //var splits = addressSpan.GetAttribute("innerHTML").Split(new[] { "<br><br>" }, StringSplitOptions.RemoveEmptyEntries);

                var headerId = driver.FindElementById("ctl00_MainPlaceHolder_lblTitle1").Text;

                string expiration = driver.FindElementById("ctl00_MainPlaceHolder_ctlBenefitPricing_lblEndDate").Text;

                string address = "";
                string phone = "";

                if (headerId.Contains("TAIZU"))
                {
                    address = "רח' מנחם בגין 23,תל אביב";
                    phone = "03 - 5225005 שלוחה 1";
                }
                else if (headerId.Contains("יין"))
                {
                    address = "דרך היין";
                    phone = "8066*";
                }
                else if (headerId.Contains("גליתא בקיבוץ צובה"))
                {
                    address = "קיבוץ צובה";
                    phone = "02-5347650";
                }
                else if (headerId.Contains("QUATTRO"))
                {
                    address = "רח' הארבעה 21 תל אביב";
                    phone = "03-9191555";
                }

                string imageSrc = driver.FindElementByCssSelector(".ImagePreview img").GetAttribute("src");


                // DB
                // location
                DataSet1.LocationsRow addressRow = locationsDataTable.NewLocationsRow();
                addressRow.Address = address;
                ExtractGeoLocation(addressRow, address);
                locationsDataTable.Rows.Add(addressRow);
                locationsAdapter.Update(locationsDataTable);

                DataSet1.American_RestuarantRow resRow = americanRrestuarantsDataTable.NewAmerican_RestuarantRow();

                resRow.Name = headerId;
                resRow.Location_LocationId = addressRow.LocationId;
                resRow.Description = description;
                resRow.Image = client.DownloadData(imageSrc);
                resRow.Phone = phone;
                resRow.Perks = perks;
                resRow.Perks = expiration;
                resRow.RankningUsersSum = 0;
                resRow.RankingsSum = 0;

                americanRrestuarantsDataTable.Rows.Add(resRow);
                amercanAdapter.Update(americanRrestuarantsDataTable);
            }
        }

        public void ExtractGeoLocation(DataSet1.LocationsRow addressRow, string address)
        {

            var requestUri = string.Format("http://maps.googleapis.com/maps/api/geocode/xml?address={0}&sensor=false", Uri.EscapeDataString(address));
            var request = WebRequest.Create(requestUri);
            var response = request.GetResponse();
            var xdoc = XDocument.Load(response.GetResponseStream());
            while (xdoc.Element("GeocodeResponse").Element("status").Value == "OVER_QUERY_LIMIT")
            {
                requestUri = string.Format("http://maps.googleapis.com/maps/api/geocode/xml?address={0}&sensor=false", Uri.EscapeDataString(address));
                request = WebRequest.Create(requestUri);
                response = request.GetResponse();
                xdoc = XDocument.Load(response.GetResponseStream());
            }
            if (xdoc.Element("GeocodeResponse").Element("status").Value == "OK")
            {
                var result = xdoc.Element("GeocodeResponse").Element("result");
                var locationElement = result.Element("geometry").Element("location");
                var lat = locationElement.Element("lat");
                var lng = locationElement.Element("lng");

                addressRow.lat = lat.Value;
                addressRow.lng = lng.Value;
            }
            else if (xdoc.Element("GeocodeResponse").Element("status").Value == "ZERO_RESULTS")
            {
                //error in address - TODO

            }
            
        }
    }
}
