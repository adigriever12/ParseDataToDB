using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ParseData.CloudTableAdapters;
using Selenium;
using System.Xml.Linq;

namespace ParseData
{
    public class ParseSelenuim
    {
        public ParseSelenuim()
        {
            ReadOnlyCollection<IWebElement> table = GetDataRowsTable();

            Cloud.CategoriesDataTable categoriesDataTable = new Cloud.CategoriesDataTable();
            Cloud.CuisinesDataTable cuisinesDataTable = new Cloud.CuisinesDataTable();
            Cloud.LocationsDataTable locationsDataTable = new Cloud.LocationsDataTable();
            Cloud.RestuarantsDataTable restuarantsDataTable = new Cloud.RestuarantsDataTable();

            CategoriesTableAdapter categoriesAdapter = new CategoriesTableAdapter();
            CuisinesTableAdapter cuisineAadapter = new CuisinesTableAdapter();
            LocationsTableAdapter locationsAdapter = new LocationsTableAdapter();
            RestuarantsTableAdapter restuarantsAdapter = new RestuarantsTableAdapter();

            WebClient client = new WebClient();
            int count = 0;

            Random rnd = new Random();
            foreach (IWebElement webRow in table)
            {
                Console.WriteLine((double)((count++) * 100) / (double)table.Count + "%");
                var currentRow = webRow.FindElements(By.TagName("td"));


                if (count > 100)
                {
                    var a = "";
                }

                #region ExtractRowData

                string resturantName = currentRow[0].FindElement(By.TagName("b")).Text;
                string description = currentRow[0].FindElement(By.TagName("span")).Text;

                string category = currentRow[1].Text;
                string cuisine = currentRow[2].Text;

                string[] row3 = currentRow[3].Text.Split('\n');

                string address = row3[0];

                string phoneNumber = "";

                if (row3.Length == 3)
                {
                    phoneNumber = row3[2];
                }

                IEnumerable<string> openingHoursEnumerable = currentRow[4].FindElements(By.TagName("span")).Select(x => x.Text);
                string openingHours = string.Join("\n", openingHoursEnumerable);

                string kosher = currentRow[5].Text;

                bool accesability = currentRow[6].FindElements(By.TagName("div")).Count != 0;

                string imgPath = currentRow[7].FindElement(By.TagName("img")).GetAttribute("src");

                #endregion


                #region Get Old Rows Data

                Cloud.CategoriesRow categoryRow = AddCategory(category, categoriesDataTable, categoriesAdapter);
                categoriesAdapter.Update(categoriesDataTable);
                #region old version

                //DataRow[] categoryDataRows = categoriesDataTable.Select(string.Format("Name = '{0}'", category));
                //if (categoryDataRows.Length == 0)
                //{
                //    categoryRow = categoriesDataTable.NewCategoriesRow();
                //    categoryRow.Name = category;
                //    categoriesDataTable.Rows.Add(categoryRow);
                //    categoriesAdapter.Update(categoriesDataTable);

                //}
                //else
                //{
                //    categoryRow = (DataSet1.CategoriesRow) categoryDataRows[0];
                //} 

                #endregion

                DataRow[] cuisinesDataRows = cuisinesDataTable.Select(string.Format("Name = '{0}'", cuisine));
                Cloud.CuisinesRow cuisineRow;

                if (cuisinesDataRows.Length == 0)
                {
                    cuisineRow = cuisinesDataTable.NewCuisinesRow();
                    cuisineRow.Name = cuisine;
                    cuisinesDataTable.Rows.Add(cuisineRow);
                    cuisineAadapter.Update(cuisinesDataTable);
                    
                }
                else
                {
                    cuisineRow = (Cloud.CuisinesRow)cuisinesDataRows[0];
                }

                #endregion
                
                var addressRow = locationsDataTable.NewLocationsRow();
                addressRow.Address = address.Replace("\r", "");

                if (!ExtractGeoLocation(addressRow, addressRow.Address.Split(',').ToList()))
                {
                    continue;
                }

                //ExtractGeoLocation(addressRow, addressRow.Address); // changes addressRow properties

                locationsDataTable.Rows.Add(addressRow);
                locationsAdapter.Update(locationsDataTable);
                
                var resturant = restuarantsDataTable.NewRestuarantsRow();
                
                resturant.Name = resturantName;
                resturant.Category_CategoryId = categoryRow.CategoryId;
                resturant.Cuisine_CuisineId = cuisineRow.CuisineId;
                resturant.Location_LocationId = addressRow.LocationId;
                resturant.Description = description;
                resturant.Kosher = kosher;
                resturant.HandicapAccessibility = accesability;
                resturant.OpeningHours = openingHours;
                resturant.Phone = phoneNumber;
                resturant.Score = rnd.Next(1, 7);

                try
                {
                    resturant.Image = client.DownloadData(imgPath);
                }
                catch (Exception) // got 404, nothing to do - image not found
                {
                    resturant.Image = null;
                }
                
                restuarantsDataTable.Rows.Add(resturant);

                restuarantsAdapter.Update(restuarantsDataTable); // TODO move outside loop
            }

            //categoriesAdapter.Update(categoriesDataTable);
            //cuisineAadapter.Update(cuisinesDataTable);
            //locationsAdapter.Update(locationsDataTable);
            //restuarantsAdapter.Update(restuarantsDataTable);

            client.Dispose();
        }

        public static Cloud.CategoriesRow AddCategory(
            string category,
            Cloud.CategoriesDataTable categoriesDataTable,
            CategoriesTableAdapter categoriesAdapter)
        {
            DataRow[] categoryDataRows = categoriesDataTable.Select(string.Format("Name = '{0}'", category.Replace("'", "")));
            Cloud.CategoriesRow categoryRow;

            if (categoryDataRows.Length == 0)
            {
                categoryRow = categoriesDataTable.NewCategoriesRow();
                categoryRow.Name = category;
                categoriesDataTable.Rows.Add(categoryRow);
                //categoriesAdapter.Update(categoriesDataTable);

            }
            else
            {
                categoryRow = (Cloud.CategoriesRow)categoryDataRows[0];
            }

            return categoryRow;
        }

        public bool ExtractGeoLocation(Cloud.LocationsRow addressRow, List<string> addresses)
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

                    addresses.RemoveAt(addresses.Count - 1);

                    address = string.Join(" ", addresses);

                }
            }

            return success;
        }

        public ReadOnlyCollection<IWebElement> GetDataRowsTable()
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

            Thread.Sleep(2000);

            return driver.FindElementsByCssSelector("#branch-table tbody tr");
        } 
        
    }
}
