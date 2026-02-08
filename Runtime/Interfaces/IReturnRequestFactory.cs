using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IReturnRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public ReturnRequest<TWidget> CreateReturnRequest();
        public bool IsRequestValid(in ReturnRequest<TWidget> request);
    }
}
