using Blue.Entity;

namespace Blue.Interface
{
    public interface ICapturable
    {
        Status Status { get; }
        EntityData EntityData { get; }
        bool IsCapturable { get; }
    }
}