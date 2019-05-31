namespace Cosmonaut.Attributes
{
    public interface ISharedCosmosCollectionInfo
    {
        string SharedCollectionName { get; }
        string EntityName { get; }
        bool UseEntityFullName { get; }
    }
}