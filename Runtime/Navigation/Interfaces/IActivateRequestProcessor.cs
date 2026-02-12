using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IActivateRequestProcessor<TWindow> where TWindow : class, IWindow
    {
        public ActivateResponse<TWindow> ProcessActivateRequest(in ActivateRequest<TWindow> request);
    }
}
