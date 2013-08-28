using System;
using System.Collections.Generic;
using System.Linq;

namespace FlexSearch.Tests.CSharp
{
    using System.Globalization;
    using System.IO;
    using CsvHelper;
    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    internal class TestDataFactory
    {
        private static List<Contact> contactRecords;
        public static List<Contact> GetContactTestData()
        {
            if (contactRecords != null)
            {
                return contactRecords;
            }

            var textReader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\FlexTestDataPeople.csv");
            var csv = new CsvReader(textReader);
            contactRecords = csv.GetRecords<Contact>().ToList();
            return contactRecords;
        }

        public static void PopulateIndexWithTestData(Interface.IIndexService indexService)
        {
            foreach (var contactRecord in GetContactTestData())
            {
                var indexDocument = new Document();
                indexDocument.Id = contactRecord.Number.ToString(CultureInfo.InvariantCulture);
                indexDocument.Index = "contact";
                indexDocument.Fields = new KeyValuePairs();

                indexDocument.Fields.Add("gender", contactRecord.Gender);
                indexDocument.Fields.Add("title", contactRecord.Title);
                indexDocument.Fields.Add("givenname", contactRecord.GivenName);
                indexDocument.Fields.Add("middleinitial", contactRecord.MiddleInitial);
                indexDocument.Fields.Add("surname", contactRecord.Surname);
                indexDocument.Fields.Add("streetaddress", contactRecord.StreetAddress);
                indexDocument.Fields.Add("city", contactRecord.City);
                indexDocument.Fields.Add("state", contactRecord.State);
                indexDocument.Fields.Add("zipcode", contactRecord.ZipCode);
                indexDocument.Fields.Add("country", contactRecord.Country);
                indexDocument.Fields.Add("countryfull", contactRecord.CountryFull);
                indexDocument.Fields.Add("emailaddress", contactRecord.EmailAddress);
                indexDocument.Fields.Add("username", contactRecord.Username);
                indexDocument.Fields.Add("password", contactRecord.Password);
                indexDocument.Fields.Add("cctype", contactRecord.CCType);
                indexDocument.Fields.Add("ccnumber", contactRecord.CCNumber);
                indexDocument.Fields.Add("cvv2", contactRecord.CVV2.ToString(CultureInfo.InvariantCulture));
                indexDocument.Fields.Add("nationalid", contactRecord.NationalID);
                indexDocument.Fields.Add("ups", contactRecord.UPS);
                indexDocument.Fields.Add("company", contactRecord.Company);
                indexDocument.Fields.Add("pounds", contactRecord.Pounds);
                indexDocument.Fields.Add("centimeters", contactRecord.Centimeters);
                indexDocument.Fields.Add("guid", contactRecord.GUID);
                indexDocument.Fields.Add("latitude", contactRecord.Latitude);
                indexDocument.Fields.Add("longitude", contactRecord.Longitude);
                indexDocument.Fields.Add("importdate", contactRecord.ImportDate);
                indexDocument.Fields.Add("timestamp", contactRecord.TimeStamp);

                indexService.PerformCommand("contact", IndexCommand.NewCreate(indexDocument.Id, indexDocument.Fields));
            }

            indexService.PerformCommand("contact", IndexCommand.Commit);
        }

        public class Contact
        {
            public int Number { get; set; }
            public string Gender { get; set; }
            public string Title { get; set; }
            public string GivenName { get; set; }
            public string MiddleInitial { get; set; }
            public string Surname { get; set; }
            public string StreetAddress { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string ZipCode { get; set; }
            public string Country { get; set; }
            public string CountryFull { get; set; }
            public string EmailAddress { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string TelephoneNumber { get; set; }
            public string MothersMaiden { get; set; }
            public string Birthday { get; set; }
            public string CCType { get; set; }
            public string CCNumber { get; set; }
            public int CVV2 { get; set; }
            public string CCExpires { get; set; }
            public string NationalID { get; set; }
            public string UPS { get; set; }
            public string Occupation { get; set; }
            public string Company { get; set; }
            public string Vehicle { get; set; }
            public string Domain { get; set; }
            public string BloodType { get; set; }
            public string Pounds { get; set; }
            public string Kilograms { get; set; }
            public string FeetInches { get; set; }
            public string Centimeters { get; set; }
            public string GUID { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public string ImportDate { get; set; }
            public string TimeStamp { get; set; }
        }


    }
}
