using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.ObjectModel;
using System.Net;
using ParseData.DataSet1TableAdapters;
using System.Data;
using System.Xml.Linq;

namespace ParseData
{
    public class ParseGrouponSelenium
    {
        public ParseGrouponSelenium()
        {
            DataSet1.CategoriesDataTable categoriesDataTable = new DataSet1.CategoriesDataTable();
            DataSet1.LocationsDataTable locationsDataTable = new DataSet1.LocationsDataTable();
            DataSet1.Groupun_RestuarantDataTable groupunRrestuarantsDataTable = new DataSet1.Groupun_RestuarantDataTable();

            CategoriesTableAdapter categoriesAdapter = new CategoriesTableAdapter();
            LocationsTableAdapter locationsAdapter = new LocationsTableAdapter();
            Groupun_RestuarantTableAdapter groupubRestuarantsAdapter = new Groupun_RestuarantTableAdapter();

            
            categoriesAdapter.Fill(categoriesDataTable);
            locationsAdapter.Fill(locationsDataTable);

            WebClient client = new WebClient();

            var driver = new ChromeDriver(@"..\..\..\");

            driver.Navigate().GoToUrl("https://www.groupon.co.il/browse/tel-aviv-iw?category=food-and-drink&category2=restaurants");

            Thread.Sleep(1000);

            var restaurantsTypes = driver.FindElementsByCssSelector(".children-list.expanded.lowest-selected li a").Select(x => new Tuple<string, string>(x.GetAttribute("href"),x.Text)).Distinct().ToList();

            foreach (Tuple<string, string> currResType in restaurantsTypes)
            {
                string category = currResType.Item2;

                Console.WriteLine("Category: " + category);

                driver.Navigate().GoToUrl(currResType.Item1);

                List<string> restaurantsLink =
                    driver.FindElementsByCssSelector("#pull-cards a").Select(x => x.GetAttribute("href")).Distinct().ToList();

                int count = 0;

                foreach (string currRes in restaurantsLink)
                {
                    Console.WriteLine((double)((count++) * 100) / (double)restaurantsLink.Count + "%");

                    driver.Navigate().GoToUrl(currRes);

                    var name = driver.FindElementByTagName("h5").Text;

                    var location = driver.FindElementsByCssSelector(".address.icon-marker-filled p").Skip(1);
                    //var city = location.ElementAt(1).Text;
                    IEnumerable<string> addresses = location.Select(x => x.Text);

                    var details = driver.FindElementByClassName("fine-print-description");


                    var detailsHtml = details.GetAttribute("innerHTML");
                    Dictionary<string, string>  smallDetails = ExtractDetails(detailsHtml);

                    string copunDescription = string.Join("*" + Environment.NewLine, driver.FindElementsByCssSelector(".twelve.columns.pitch ul li").Select(x => x.Text));

                    var description = driver.FindElementByCssSelector(".nutshell.highlights p").Text;

                    if (!smallDetails.ContainsKey("kosher") && description.Contains("כשר"))
                    {
                        smallDetails["kosher"] = "כשר";
                    }

                    var imageSrc = driver.FindElementByCssSelector(".gallery-featured img").GetAttribute("src");

                    // category
                    DataSet1.CategoriesRow categoryRow = categoriesDataTable.NewCategoriesRow();
                    categoryRow.Name = category;
                    categoriesDataTable.Rows.Add(categoryRow);
                    categoriesAdapter.Update(categoriesDataTable);

                    // location
                    DataSet1.LocationsRow addressRow = locationsDataTable.NewLocationsRow();
                    addressRow.Address = String.Join(" ", addresses);

                    if (!ExtractGeoLocation(addressRow, addresses.ToList()))
                    {
                        continue;
                    }

                    locationsDataTable.Rows.Add(addressRow);
                    locationsAdapter.Update(locationsDataTable);

                    DataSet1.Groupun_RestuarantRow resRow = groupunRrestuarantsDataTable.NewGroupun_RestuarantRow();

                    resRow.Name = name;
                    resRow.Category_CategoryId = categoryRow.CategoryId;
                    resRow.Location_LocationId = addressRow.LocationId;
                    resRow.Description = description;
                    resRow.CopunDescription = copunDescription;
                    resRow.Image = client.DownloadData(imageSrc);
                    resRow.Kosher = smallDetails.ContainsKey("kosher") ? smallDetails["kosher"] : "";
                    resRow.Expiration = smallDetails.ContainsKey("expiration") ? smallDetails["expiration"] : "";
                    resRow.Hours = smallDetails.ContainsKey("hours") ? smallDetails["hours"] : "";
                    resRow.PhoneAndContent = smallDetails.ContainsKey("phone") ? smallDetails["phone"] : "";

                    groupunRrestuarantsDataTable.Rows.Add(resRow);
                    groupubRestuarantsAdapter.Update(groupunRrestuarantsDataTable);
                }
            }

            //categoriesAdapter.Update(categoriesDataTable);
            //locationsAdapter.Update(locationsDataTable);
            
            //groupubRestuarantsAdapter.upda
            //groupubRestuarantsAdapter.Adapter.upda.Adapter.Update(groupunRrestuarantsDataTable);
        }

        private Dictionary<string, string> ExtractDetails(string detailsHtml)
        {
            string[] paragraphs = detailsHtml.Split(new[] { "<strong>" }, StringSplitOptions.RemoveEmptyEntries);
            string phone = "", kosher = "";

            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach (string paragraph in paragraphs)
            {
                var p = paragraph.Split(new[] { "</strong>" }, StringSplitOptions.RemoveEmptyEntries);

                string title = p[0];
                var content = p[1].Replace("<br>", "");

                if (content.Contains("כשר"))
                {
                    kosher = content.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).First(x => x.Contains("כשר"));

                    content = content.Replace(kosher, "");

                    results["kosher"] = kosher;
                }

                // expiration
                if (title.Contains("תקף") || title.Contains("בתוקף"))
                {
                    results["expiration"] = content;
                }
                else if (title.Contains("שעות"))
                {
                    if (content.Contains("טלפון"))
                    {
                        phone = content.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).First(x => x.Contains("טלפון"));
                        content = content.Replace(phone, "");

                        results["phone"] = phone;
                    }

                    results["hours"] = content;
                }
                else if (title.Contains("בטלפון"))
                {
                    phone = content;
                    results["phone"] = content;
                }
            }

            return results;
        }

        private string ExtractKosher(string data)
        {
            string[] rows = data.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string row in rows)
            {
                if (row.Contains("כשר"))
                {
                    return row;
                }
            }

            return "";
        }

        private static void IntializeDBAdapters(out DataSet1.LocationsDataTable locationsDataTable, out LocationsTableAdapter locationsAdapter)
        {
            DataSet1.CategoriesDataTable categoriesDataTable = new DataSet1.CategoriesDataTable();
            DataSet1.CuisinesDataTable cuisinesDataTable = new DataSet1.CuisinesDataTable();
            locationsDataTable = new DataSet1.LocationsDataTable();
            DataSet1.RestuarantsDataTable restuarantsDataTable = new DataSet1.RestuarantsDataTable();

            CategoriesTableAdapter categoriesAdapter = new CategoriesTableAdapter();
            CuisinesTableAdapter cuisineAadapter = new CuisinesTableAdapter();
            locationsAdapter = new LocationsTableAdapter();
            RestuarantsTableAdapter restuarantsAdapter = new RestuarantsTableAdapter();
        }

        public static bool ExtractGeoLocation(DataSet1.LocationsRow addressRow, List<string> addresses)
        {
            string address = string.Join(" ", addresses);
            bool success = false;

            while (!success)
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

                    success = true;
                }
                else if (xdoc.Element("GeocodeResponse").Element("status").Value == "ZERO_RESULTS")
                {
                    //error in address - TODO
                    if (addresses.Count == 1)
                    {
                        return false;
                    }

                    addresses.RemoveAt(1);

                    address = string.Join(" ", addresses);
                    
                }
            }

            return success;
        }
    }
}
