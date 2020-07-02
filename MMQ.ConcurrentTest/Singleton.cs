using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace MMQ.ConcurrentTest
{
    public class Singleton
    {
        /// <summary>
        /// 静态变量(用来存放类的实例)
        /// </summary>
        private static Singleton _instance;

        /// <summary>
        /// 用来锁定的对象
        /// </summary>
        private static readonly object locker = new object();

        /// <summary>
        /// 静态属性(提供给外部的全局访问点)
        /// </summary>
        public static Singleton Instance
        {
            get
            {
                //只有当第一次创建实例的时候再执行lock语句
                if (_instance != null) return _instance;

                //当一个线程执行的时候，会先检测locker对象是否是锁定状态，
                //如果不是则会对locker对象锁定，如果是则该线程就会挂起等待
                //locker对象解锁,lock语句执行运行完之后会对该locker对象解锁
                lock (locker)
                {
                    if (_instance != null) return _instance;
                    Console.WriteLine($"create new Singleton");
                    _instance = new Singleton();
                }
                return _instance;
            }
        }

        private readonly DateTime _startTime;

        private double _useTime = 0;

        /// <summary>
        /// 私有构造方法(防止被外部实例化)
        /// </summary>
        private Singleton()
        {
            _startTime = DateTime.Now;
            new Thread(Run).Start();
        }

        private static readonly IMemoryMappedQueue Queue = MemoryMappedQueue.CreateOrOpen("UniqueName", 1 * 1024 * 1024);

        private int _uId = 0;
        private int _sharedMemoryId = 0;

        public void SendMessage(MessageStructure messageStructure, out string sharedMemoryName)
        {
            sharedMemoryName = messageStructure.SharedMemoryName = $"BrowserCommand{messageStructure.ManagedThreadId.ToString().PadLeft(3, '0')}{(++_sharedMemoryId).ToString().PadLeft(6, '0')}";

            var producer = Queue.CreateProducer();

            var message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageStructure));

            producer.Enqueue(message);

            Console.WriteLine($"SendMessage : {JsonConvert.SerializeObject(messageStructure)}");
        }

        private void Run()
        {
            using (var consumer = MemoryMappedQueue.CreateConsumer("UniqueName"))
            {
                while (true)
                {
                    if (!consumer.TryDequeue(out var message)) continue;
                    var text = Encoding.UTF8.GetString(message);

                    var messageStructure = JsonConvert.DeserializeObject<MessageStructure>(text);
                    ++_uId;
                    ResultData(messageStructure);
                }
            }
        }

        private void ResultData(MessageStructure messageStructure)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(50);
                _useTime = (DateTime.Now - _startTime).TotalMilliseconds;
                messageStructure.ResultMessage = $"ResultData -> uId : {_uId} , useTime : {_useTime}";
                messageStructure.IsTimeout = false;
            }).Wait(messageStructure.TimeoutMillisecond);

            var queue = MemoryMappedQueue.CreateOrOpen(messageStructure.SharedMemoryName);
            var producer = queue.CreateProducer();

            var message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageStructure));

            producer.Enqueue(message);


        }
    }
}