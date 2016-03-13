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
using ParseData.DataSet1TableAdapters;
using Selenium;

namespace ParseData
{
    public class ParseSelenuim
    {
        public ParseSelenuim()
        {
            ReadOnlyCollection<IWebElement> table = GetDataRowsTable();

            DataSet1.CategoriesDataTable categoriesDataTable = new DataSet1.CategoriesDataTable();
            DataSet1.CuisinesDataTable cuisinesDataTable = new DataSet1.CuisinesDataTable();
            DataSet1.LocationsDataTable locationsDataTable = new DataSet1.LocationsDataTable();
            DataSet1.RestuarantsDataTable restuarantsDataTable = new DataSet1.RestuarantsDataTable();

            CategoriesTableAdapter categoriesAdapter = new CategoriesTableAdapter();
            CuisinesTableAdapter cuisineAadapter = new CuisinesTableAdapter();
            LocationsTableAdapter locationsAdapter = new LocationsTableAdapter();
            RestuarantsTableAdapter restuarantsAdapter = new RestuarantsTableAdapter();

            WebClient client = new WebClient();

            foreach (IWebElement webRow in table)
            {
                var currentRow = webRow.FindElements(By.TagName("td"));

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

                bool accesability = currentRow[6].Text != "";

                string imgPath = currentRow[7].FindElement(By.TagName("img")).GetAttribute("src");

                #endregion


                #region Get Old Rows Data

                DataRow[] categoryDataRows = categoriesDataTable.Select(string.Format("Name = '{0}'", category));
                DataSet1.CategoriesRow categoryRow;

                if (categoryDataRows.Length == 0)
                {
                    categoryRow = categoriesDataTable.NewCategoriesRow();
                    categoryRow.Name = category;
                    categoriesDataTable.Rows.Add(categoryRow);
                    categoriesAdapter.Update(categoriesDataTable);
                    
                }
                else
                {
                    categoryRow = (DataSet1.CategoriesRow) categoryDataRows[0];
                }

                DataRow[] cuisinesDataRows = cuisinesDataTable.Select(string.Format("Name = '{0}'", cuisine));
                DataSet1.CuisinesRow cuisineRow;

                if (cuisinesDataRows.Length == 0)
                {
                    cuisineRow = cuisinesDataTable.NewCuisinesRow();
                    cuisineRow.Name = cuisine;
                    cuisinesDataTable.Rows.Add(cuisineRow);
                    cuisineAadapter.Update(cuisinesDataTable);
                    
                }
                else
                {
                    cuisineRow = (DataSet1.CuisinesRow)cuisinesDataRows[0];
                }

                #endregion
                
                var addressRow = locationsDataTable.NewLocationsRow();
                addressRow.Address = address;
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

                try
                {
                    resturant.Image = client.DownloadData(imgPath);
                }
                catch (Exception) // got 404, nothing to do - image not found
                {
                    resturant.Image = null;
                }
                
                restuarantsDataTable.Rows.Add(resturant);


                restuarantsAdapter.Update(restuarantsDataTable);
            }

            client.Dispose();
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
            return driver.FindElementsByCssSelector("#branch-table tbody tr");
        } 
        
    }
}
