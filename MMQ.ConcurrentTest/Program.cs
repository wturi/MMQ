using System;
using System.Text;
using System.Threading;

namespace MMQ.ConcurrentTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine($"Memory Queue Test Start");
            //SingletonRun();

            new Thread(() => { TestEnqueue("Enqueue1"); }).Start();
            new Thread(() => { TestEnqueue("Enqueue2"); }).Start();

            Thread.Sleep(100);

            new Thread(() => { TestDequeue("Dequeue1"); }).Start();

            new Thread(() => { TestDequeue("Dequeue2"); }).Start();


            Thread.Sleep(20000);

            Console.WriteLine($"thread3 write");

            new Thread(() => { TestEnqueue("Enqueue3"); }).Start();

            Console.ReadLine();
        }

        #region 测试

        private static void TestEnqueue(string threadNum)
        {
            using (var queue = MemoryMappedQueue.CreateOrOpen("UniqueName", 1 * 1024 * 1024))
            {
                using (var producer = queue.CreateProducer())
                {
                    var num = 1;
                    while (num < 200)
                    {
                        var test = $"Hello,{threadNum}!{++num}";
                        var message = Encoding.UTF8.GetBytes(test);
                        producer.Enqueue(message);
                        Console.WriteLine($"{threadNum} enqueue to memory : {test}");

                        Thread.Sleep(100);
                    }
                }
            }
        }

        private static void TestDequeue(string threadNum)
        {
            using (var consumer = MemoryMappedQueue.CreateConsumer("UniqueName"))
            {
                while (true)
                {
                    if (consumer.TryDequeue(out var message))
                    {
                        var text = Encoding.UTF8.GetString(message);
                        Console.WriteLine($"\t\t\t\t\t\t {threadNum} read message from memory : {text}");
                        Thread.Sleep(50);
                    }
                }
            }
        }

        #endregion 测试
    }
}