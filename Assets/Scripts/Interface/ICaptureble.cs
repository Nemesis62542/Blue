using Blue.Entity;
using Blue.Item;

namespace Blue.Interface
{
    public interface ICapturable
    {
        Status Status { get; }
        ItemData CapturedItem { get; }
    }
}