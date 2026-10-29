namespace UIFramework.Core.Interfaces
{
    public delegate void WindowAction(IWindow window);
    public delegate void WindowIndexAction(IWindow window, int index);
    
    public interface IReadOnlyWindow : IReadOnlyWidget { }
    
    public interface IWindow : IReadOnlyWindow, IWidget { }
}