using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IActivateRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        public ActivateResponse<TWidget> ProcessActivateRequest(in ActivateRequest<TWidget> request);
    }
}
