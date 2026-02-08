using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IActivateRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        public ActivateResponse<TWidget> ProcessActivateRequest(in ActivateRequest<TWidget> request);
    }
}
