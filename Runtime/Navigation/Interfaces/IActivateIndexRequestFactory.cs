using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IActivateIndexRequestFactory<TWindow> where TWindow : class, IWindow
    {
        public ActivateRequest<TWindow> CreateActivateRequest(int index); 
    }
}
