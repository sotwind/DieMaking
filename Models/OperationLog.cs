namespace DieMaking.Models;

public class OperationLog
{
    public int LogID { get; set; }
    public int? UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string OperationDesc { get; set; } = string.Empty;
    public int? DieID { get; set; }
    public DateTime CreateTime { get; set; }
}
