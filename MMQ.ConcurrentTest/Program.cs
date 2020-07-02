using Newtonsoft.Json;

using System;
using System.Text;
using System.Threading;

using Console = System.Console;

namespace MMQ.ConcurrentTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine($"Memory Queue Test Start");
            //SingletonRun();

            new Thread(() =>
            {
                new Thread(() => { TestEnqueue("Enqueue1", 1000); }).Start();
                new Thread(() => { TestEnqueue("Enqueue2", 2000); }).Start();
                new Thread(() => { TestEnqueue("Enqueue3", 3000); }).Start();
                new Thread(() => { TestEnqueue("Enqueue4", 4000); }).Start();
                new Thread(() => { TestEnqueue("Enqueue5", 5000); }).Start();
            }).Start();

            new Thread(() =>
            {
                new Thread(() => { TestEnqueue("Enqueue6", 6000); }).Start();
                new Thread(() => { TestEnqueue("Enqueue7", 7000); }).Start();
                new Thread(() => { TestEnqueue("Enqueue8", 8000); }).Start();
                new Thread(() => { TestEnqueue("Enqueue8", 9000); }).Start();
                new Thread(() => { TestEnqueue("Enqueue8", 10000); }).Start();
            }).Start();

            Console.ReadLine();
        }

        #region 测试

        //传入命令消息
        private static void TestEnqueue(string threadNum, int num)
        {
            for (var i = 0; i < 100; i++)
            {
                var singDemo = Singleton.Instance;

                var message = new MessageStructure()
                {
                    ManagedThreadId = Thread.CurrentThread.ManagedThreadId,
                    CommandMessage = $"{threadNum}--{num + i + 1}",
                    TimeoutMillisecond = 5000
                };

                singDemo.SendMessage(message, out var sharedMemoryName);

                var startTime = DateTime.Now;

                using (var queue = MemoryMappedQueue.CreateOrOpen(sharedMemoryName))
                {
                    while (DateTime.Now - startTime < TimeSpan.FromMilliseconds(message.TimeoutMillisecond + 1000))
                    {
                        try
                        {
                            using (var consumer = MemoryMappedQueue.CreateConsumer(sharedMemoryName))
                            {
                                if (!consumer.TryDequeue(out var resultMessage)) continue;
                                var text = Encoding.UTF8.GetString(resultMessage);
                                var messageObj = JsonConvert.DeserializeObject<MessageStructure>(text);
                                if (messageObj.IsTimeout)
                                {
                                    Console.WriteLine($"[{threadNum}] time out :{text} ");
                                    break;
                                }
                                Console.WriteLine($"[{threadNum}] TestEnqueue : {text}");
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                }
            }
        }

        #endregion 测试
    }
}