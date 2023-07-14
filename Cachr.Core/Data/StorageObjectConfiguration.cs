namespace Cachr.Core.Data;

public class StorageObjectConfiguration
{
    public string ConnectionString { get; set; } = "Data Source=./objects.db";
    public string BasePath { get; set; } = "./data";
}
