namespace MMQ.ConcurrentTest
{
    public class MessageStructure
    {
        public int ManagedThreadId { get; set; }

        public string CommandMessage { get; set; }

        public string ResultMessage { get; set; }

        public bool IsTimeout { get; set; } = true;

        public int TimeoutMillisecond { get; set; } = 5000;

        public string SharedMemoryName { get; set; }
    }
}