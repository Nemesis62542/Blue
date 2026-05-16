using Blue.UI.Screen;

namespace Blue.UI
{
    public interface IScreenController
    {
        void OnScreenChanged(ScreenState state);
    }
}
