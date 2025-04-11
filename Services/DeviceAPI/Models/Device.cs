
namespace DeviceAPI.Models
{
    public class Device
    {

        public string Id { get; set; }
        public string MACAddress { get; set; }
        public string IPAddress { get; set; }
        public string DeviceName { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

    }
}
