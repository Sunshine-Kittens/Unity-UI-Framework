using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IActivateRequestFactory<TWindow> where TWindow : class, IWindow
    {
        public ActivateRequest<TWindow> CreateActivateRequest(TWindow window);
        public ActivateRequest<TWindow> CreateActivateRequest<TTarget>() where TTarget : class, TWindow; 
    }
}
