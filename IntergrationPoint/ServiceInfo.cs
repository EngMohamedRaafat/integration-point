namespace IntergrationPoint
{
    class ServiceInfo
    {
        public string name { get; private set; }
        public int count { get; private set; }

        public ServiceInfo(string info, int c)
        {
            this.name = info;
            this.count = c;
        }
        public void increaseCount() => count++;
    }
}