using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IReturnRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        public NavigationResponse<TWidget> ProcessReturnRequest(in ReturnRequest<TWidget> request);
    }
}
