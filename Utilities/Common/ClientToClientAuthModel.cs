namespace MonitoringSystem.Utilities.Common
{
    public class ClientToClientAuthModel
    {
        //public int ToModuleId { get; set; }
        public int? TypeId { get; set; }
        public string AuthToken { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string? Payload { get; set; }

        //public bool? OptionalBoolean { get; set; }
    }
}
