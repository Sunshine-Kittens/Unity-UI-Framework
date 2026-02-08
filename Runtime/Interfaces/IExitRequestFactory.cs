using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IExitRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public ExitRequest<TWidget> CreateExitRequest();
        public bool IsRequestValid(in  ExitRequest<TWidget> request);
    }
}
