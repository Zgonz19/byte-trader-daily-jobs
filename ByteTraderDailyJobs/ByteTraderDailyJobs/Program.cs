using Microsoft.Extensions.Configuration;
using System;

namespace ByteTraderDailyJobs
{
    class Program
    {



        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                   new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
        }

        static void Main(string[] args)
        {
            //public DateTime(int year, int month, int day, int hour, int minute, int second);
            //var date = new DateTime(2021, 08, 01, 18, 0, 0);
            //var unixTimeStampInMilliseconds = DateTimeToUnixTimestamp(date);

            //test branch
            var dailyTaskApp = new InitializeApp();
            //InitializeApp.MadeupFunction();
            dailyTaskApp.ExecuteProcessList();
            //TestCommit
            //bool isCatAlive = true; // true

            //decimal testval1 = 323435;
            //double testval2 = 2232345.23492735;
            //float testval3 = 232452;


            //var bool1 = true;
            //var bool2 = false;
            //bool output;

            //if : 
            //    print("text")
            //    adsfhjl;args
            //    asdfa
            //asdfa

            //else
            //    print("text2");

            //if(bool1 == bool2)
            //{
            //    print("text")
            //    adsfhjl; args
            //    asdfa
                
            //    Console.WriteLine("They are the same");
            //}
            //else
            //{
            //    Console.WriteLine("They are not equal");
            //}

            //while(bool1 == bool2)
            //{
            //    //execute this code
            //}

            //Customer object1 = new Customer();
            //object1.SetCustomerAge(6);

            //int x = object1.Age;



            //object1.Age = 28;
            //object1.FirstName = "Gonzalo";
            //object1.Address "asdfasdf";

            //Customer object2 = new Customer();


            //var test6 = object2.TestMethod1(8, "Elisabeth"); //SampleElisabeth
            //test6 = "SampleElisabeth";

            //var xx = object1.Age;
            //int y = 28;
            //object1.Age = y;

            //Customer object3 = new Customer(3, "safds");
        }
    }


    public class Customer
    {
        public string FirstName;
        public int Age;
        public string Address;

        public Customer(int age, string name)
        {
            Age = age;
            FirstName = name;
        }

        public void SetCustomerAge(int age, string name)
        {
            Age = age;
        }


        public string TestMethod1(int age, string name)
        {
            Age = age;
            var x = "Sample";

            return (x+name);
        }

        public int AddAges(int age, int age2)
        {
            var x = (age + age2);
            return x;
        }

        // Fields, properties, methods and events go here...
    }

}
