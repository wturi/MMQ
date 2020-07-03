using Newtonsoft.Json;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MMQ.ConcurrentTest
{
    public class Singleton
    {
        private static Singleton _instance;

        private static readonly object locker = new object();

        public static Singleton Instance
        {
            get
            {
                if (_instance != null) return _instance;

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
                //模拟逻辑处理所用时间
                {
                    Thread.Sleep(20);
                }

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