namespace IoTRynningeasenFACore.Models
{
    public class Sensor<T>
    {
        public Config config { get; set; }
        public int ep { get; set; }
        public string etag { get; set; }
        public string manufacturername { get; set; }
        public string modelid { get; set; }
        public string name { get; set; }
        public T state { get; set; }
        public string swversion { get; set; }
        public string type { get; set; }
        public string uniqueid { get; set; }
    }
}