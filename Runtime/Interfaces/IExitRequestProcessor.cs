using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IExitRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        public ExitResponse ProcessExitRequest(in ExitRequest<TWidget> request);
    }
}
