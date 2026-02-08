using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IActivateIndexRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public ActivateRequest<TWidget> CreateActivateRequest(int index); 
    }
}
