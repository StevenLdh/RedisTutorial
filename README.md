一、Redis简介以及安装
1、Redis简介
Redis是一个开源的，使用C语言编写，面向“键/值”对类型数据的分布式NoSQL数据库系统，特点是高性能，持久存储，适应高并发的应用场景。Redis纯粹为应用而产生，它是一个高性能的key-value数据库,并且提供了多种语言的API性能测试结果表示SET操作每秒钟可达110000次，GET操作每秒81000次（当然不同的服务器配置性能不同）。
Redis目前提供五种数据类型：string(字符串),list（链表）, Hash（哈希）,set（集合）及zset(sorted set) （有序集合）。
Redis开发维护很活跃，虽然它是一个Key-Value数据库存储系统，但它本身支持MQ功能，所以完全可以当做一个轻量级的队列服务来使用。对于RabbitMQ和Redis的入队和出队操作，各执行100万次，每10万次记录一次执行时间。测试数据分为128Bytes、512Bytes、1K和10K四个不同大小的数据。实验表明：入队时，当数据比较小时Redis的性能要高于RabbitMQ，而如果数据大小超过了10K，Redis则慢的无法忍受；出队时，无论数据大小，Redis都表现出非常好的性能，而RabbitMQ的出队性能则远低于Redis。

2、Redis与Memcached的比较.

1、Memcached是多线程，而Redis使用单线程.
2、Memcached使用预分配的内存池的方式，Redis使用现场申请内存的方式来存储数据，并且可以配置虚拟内存。
3、Redis可以实现持久化，主从复制，实现故障恢复。
4、Memcached只是简单的key与value,但是Redis支持数据类型比较多。
Redis的存储分为内存存储、磁盘存储 .从这一点，也说明了Redis与Memcached是有区别的。Redis 与Memcached一样，为了保证效率，数据都是缓存在内存中。区别的是redis会周期性的把更新的数据写入磁盘或者把修改 操作写入追加的记录文件，并且在此基础上实现了master-slave(主从)同步。
Redis有两种存储方式，默认是snapshot方式，实现方法是定时将内存的快照(snapshot)持久化到硬盘，这种方法缺点是持久化之后如果出现crash则会丢失一段数据。因此在完美主义者的推动下作者增加了aof方式。aof即append
only mode，在写入内存数据的同时将操作命令保存到日志文件，在一个并发更改上万的系统中，命令日志是一个非常庞大的数据，管理维护成本非常高，恢复重建时间会非常长，这样导致失去aof高可用性本意。另外更重要的是Redis是一个内存数据结构模型，所有的优势都是建立在对内存复杂数据结构高效的原子操作上，这样就看出aof是一个非常不协调的部分。
其实aof目的主要是数据可靠性及高可用性.

3、Redis安装

文章的最后我提供了下载包，当然你也可以去官网下载最新版本的Redis
https://github.com/dmajkic/redis/downloads 将服务程序拷贝到一个磁盘上的目录，如下图：

这里写图片描述

文件说明： redis-server.exe：服务程序
redis-check-dump.exe：本地数据库检查
redis-check-aof.exe：更新日志检查
redis-benchmark.exe：性能测试，用以模拟同时由N个客户端发送M个
SETs/GETs 查询.
redis-cli.exe： 服务端开启后，我们的客户端就可以输入各种命令测试了

1、打开一个cmd窗口，使用cd命令切换到指定目录（F:\Redis）运行 redis-server.exe redis.conf
2、重新打开一个cmd窗口，使用cd命令切换到指定目录（F:\Redis）运行 redis-cli.exe -h 127.0.0.1 -p 6379，其中 127.0.0.1是本地ip，6379是redis服务端的默认端口 （这样可以开启一个客户端程序进行特殊指令的测试）.
可以将此服务设置为windows系统服务，下载Redis服务安装软件，安装即可。（https://github.com/rgl/redis/downloads） 如果你的电脑是64bit系统，可以下载redis-2.4.6-setup-64-bit.exe

二、项目使用
1、首先部署环境
创建相应的解决方案，然后添加引用和文件
在这里插入图片描述
2、然后增加配置
在app.config配置相应的路径

<?xml version="1.0"?>
<configuration>
  <configSections>
    <section name="RedisConfig" type="RedisTutorial.RedisConfigInfo, RedisTutorial"/>
  </configSections>
  <RedisConfig WriteServerList="127.0.0.1:6379" ReadServerList="127.0.0.1:6379" MaxWritePoolSize="60" MaxReadPoolSize="60"
               AutoStart="true" LocalCacheTime="180" RecordeLog="false">
  </RedisConfig>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.0"/>
  </startup>
</configuration>
3、实现访问基类
1、RedisManager.cs主要调用类

public class RedisManager
    {
        /// <summary>
        /// redis配置文件信息
        /// </summary>
        private static RedisConfigInfo redisConfigInfo = RedisConfigInfo.GetConfig();

        private static PooledRedisClientManager prcm;

        /// <summary>
        /// 静态构造方法，初始化链接池管理对象
        /// </summary>
        static RedisManager()
        {
            CreateManager();
        }


        /// <summary>
        /// 创建链接池管理对象
        /// </summary>
        private static void CreateManager()
        {
            string[] writeServerList = SplitString(redisConfigInfo.WriteServerList, ",");
            string[] readServerList = SplitString(redisConfigInfo.ReadServerList, ",");

            prcm = new PooledRedisClientManager(readServerList, writeServerList,
                             new RedisClientManagerConfig
                             {
                                 MaxWritePoolSize = redisConfigInfo.MaxWritePoolSize,
                                 MaxReadPoolSize = redisConfigInfo.MaxReadPoolSize,
                                 AutoStart = redisConfigInfo.AutoStart,
                             });
        }

        private static string[] SplitString(string strSource, string split)
        {
            return strSource.Split(split.ToArray());
        }

        /// <summary>
        /// 客户端缓存操作对象
        /// </summary>
        public static IRedisClient GetClient()
        {
            if (prcm == null)
                CreateManager();

            return prcm.GetClient();
        }

    }
2、RedisConfigInfo.cs获取相应的配置信息类

public sealed class RedisConfigInfo : ConfigurationSection
    {
        public static RedisConfigInfo GetConfig()
        {
            RedisConfigInfo section = (RedisConfigInfo)ConfigurationManager.GetSection("RedisConfig");
            return section;
        }

        public static RedisConfigInfo GetConfig(string sectionName)
        {
            RedisConfigInfo section = (RedisConfigInfo)ConfigurationManager.GetSection("RedisConfig");
            if (section == null)
                throw new ConfigurationErrorsException("Section " + sectionName + " is not found.");
            return section;
        }
        /// <summary>
        /// 可写的Redis链接地址
        /// </summary>
        [ConfigurationProperty("WriteServerList", IsRequired = false)]
        public string WriteServerList
        {
            get
            {
                return (string)base["WriteServerList"]; 
            }
            set
            {
                base["WriteServerList"] = value;
            }
        }

        
        /// <summary>
        /// 可读的Redis链接地址
        /// </summary>
        [ConfigurationProperty("ReadServerList", IsRequired = false)]
        public string ReadServerList
        {
            get
            {
                return (string)base["ReadServerList"]; 
            }
            set
            {
                base["ReadServerList"] = value;
            }
        }

        
        /// <summary>
        /// 最大写链接数
        /// </summary>
        [ConfigurationProperty("MaxWritePoolSize", IsRequired = false, DefaultValue = 5)]
        public int MaxWritePoolSize
        {
            get
            {
                int _maxWritePoolSize = (int)base["MaxWritePoolSize"];
                return _maxWritePoolSize > 0 ? _maxWritePoolSize : 5;
            }
            set
            {
                base["MaxWritePoolSize"] = value;
            }
        }

       
        /// <summary>
        /// 最大读链接数
        /// </summary>
        [ConfigurationProperty("MaxReadPoolSize", IsRequired = false, DefaultValue = 5)]
        public int MaxReadPoolSize
        {
            get
            {
                int _maxReadPoolSize = (int)base["MaxReadPoolSize"];
                return _maxReadPoolSize > 0 ? _maxReadPoolSize : 5;
            }
            set
            {
                base["MaxReadPoolSize"] = value;
            }
        }

         
        /// <summary>
        /// 自动重启
        /// </summary>
        [ConfigurationProperty("AutoStart", IsRequired = false, DefaultValue = true)]
        public bool AutoStart
        {
            get
            {
                return (bool)base["AutoStart"];
            }
            set
            {
                base["AutoStart"] = value;
            }
        }


        
        /// <summary>
        /// 本地缓存到期时间，单位:秒
        /// </summary>
        [ConfigurationProperty("LocalCacheTime", IsRequired = false, DefaultValue = 36000)]
        public int LocalCacheTime
        {
            get
            {
                return (int)base["LocalCacheTime"];
            }
            set
            {
                base["LocalCacheTime"] = value;
            }
        }

       
        /// <summary>
        /// 是否记录日志,该设置仅用于排查redis运行时出现的问题,如redis工作正常,请关闭该项
        /// </summary>
        [ConfigurationProperty("RecordeLog", IsRequired = false, DefaultValue = false)]
        public bool RecordeLog
        {
            get
            {
                return (bool)base["RecordeLog"];
            }
            set
            {
                base["RecordeLog"] = value;
            }
        }        
    }
3、TestRedis.cs使用实例(部分代码)

/// <summary>
/// 测试redis
/// </summary>
    public static void GetRedisData() {
        using (var redisClient = RedisManager.GetClient())
        {
            using (var cars = redisClient.GetTypedClient<Car>())
            {
               //获取所有数数据并清空
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
               //睡眠6秒
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
以上是部分核心代码，如需获取具体代码地址为：
https://github.com/StevenLdh/RedisTutorial.git
学海无涯，粗浅学识，相互学习，互相进步！
