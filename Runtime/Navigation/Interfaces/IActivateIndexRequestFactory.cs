using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IActivateIndexRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public ActivateRequest<TWidget> CreateActivateRequest(int index); 
    }
}
