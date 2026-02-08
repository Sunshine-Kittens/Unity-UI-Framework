using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IActivateRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public ActivateRequest<TWidget> CreateActivateRequest(TWidget widget);
        public ActivateRequest<TWidget> CreateActivateRequest<TTarget>() where TTarget : class, TWidget; 
    }
}
