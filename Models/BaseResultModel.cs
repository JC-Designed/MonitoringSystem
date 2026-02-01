namespace MonitoringSystem.Models
{
    public class BaseResultModel
    {
        public string Message { get; set; }
        public string HttpCode { get; set; }
        public bool IsSuccess { get; set; }

    }
}
