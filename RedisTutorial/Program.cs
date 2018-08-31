using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;
using System.Threading;

namespace RedisTutorial
{
    class Program
    {
        static void Main(string[] args)
        {
            //var redisClient = new RedisClient("localhost");
            using (var redisClient = RedisManager.GetClient())
            {
                using (var cars = redisClient.GetTypedClient<Car>())
                {
                    if (cars.GetAll().Count > 0)
                        cars.DeleteAll();

                    var dansFord = new Car
                    {
                        Id = cars.GetNextSequence(),
                        Title = "Dan's Ford",
                        Make = new Make { Name = "Ford" },
                        Model = new Model { Name = "Fiesta" }
                    };
                    var beccisFord = new Car
                    {
                        Id = cars.GetNextSequence(),
                        Title = "Becci's Ford",
                        Make = new Make { Name = "Ford" },
                        Model = new Model { Name = "Focus" }
                    };
                    var vauxhallAstra = new Car
                    {
                        Id = cars.GetNextSequence(),
                        Title = "Dans Vauxhall Astra",
                        Make = new Make { Name = "Vauxhall" },
                        Model = new Model { Name = "Asta" }
                    };
                    var vauxhallNova = new Car
                    {
                        Id = cars.GetNextSequence(),
                        Title = "Dans Vauxhall Nova",
                        Make = new Make { Name = "Vauxhall" },
                        Model = new Model { Name = "Nova" }
                    };

                    var carsToStore = new List<Car> { dansFord, beccisFord, vauxhallAstra, vauxhallNova };
                    cars.StoreAll(carsToStore);

                    Console.WriteLine("Redis Has-> " + cars.GetAll().Count + " cars");


                    cars.ExpireAt(vauxhallAstra.Id, DateTime.Now.AddSeconds(5)); //Expire Vauxhall Astra in 5 seconds

                    Thread.Sleep(6000); //Wait 6 seconds to prove we can expire our old Astra

                    Console.WriteLine("Redis Has-> " + cars.GetAll().Count + " cars");


                    //Get Cars out of Redis
                    var carsFromRedis = cars.GetAll().Where(car => car.Make.Name == "Ford");

                    foreach (var car in carsFromRedis)
                    {
                        Console.WriteLine("Redis Has a ->" + car.Title);
                    }
                   

                }
            }
            Console.ReadLine();
        }
    }

    public class Car
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Make Make { get; set; }
        public Model Model { get; set; }
    }

    public class Make
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Model
    {
        public int Id { get; set; }
        public Make Make { get; set; }
        public string Name { get; set; }
    }

}
