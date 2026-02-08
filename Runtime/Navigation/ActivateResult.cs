namespace UIFramework.Navigation
{
    public readonly struct ActivateResult<TWidget> where TWidget : class, IWidget
    {
        public readonly bool Success;
        public readonly TWidget Active;
        public readonly int Index;

        public ActivateResult(bool success, TWidget active, int index)
        {
            Success = success;
            Active = active;
            Index = index;
        }
    }
}
