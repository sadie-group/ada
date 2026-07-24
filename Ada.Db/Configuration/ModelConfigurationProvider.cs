namespace Ada.Db.Configuration;

public static class ModelConfigurationProvider
{
    public static IModelConfiguration Active { get; set; } = new DefaultModelConfiguration();
}
