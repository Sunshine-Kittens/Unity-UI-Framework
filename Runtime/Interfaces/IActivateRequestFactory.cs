using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IActivateRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public ActivateRequest<TWidget> CreateActivateRequest(TWidget widget);
        public ActivateRequest<TWidget> CreateActivateRequest<TTarget>() where TTarget : class, TWidget; 
    }
}
