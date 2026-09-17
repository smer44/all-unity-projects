public sealed class DefaultUpperBodyState : AbstractUpperBodyState
{
    public DefaultUpperBodyState(UpperBodyVisualsController controller) : base(controller) { }

    public override void OnEnter()
    {
        Controller.SetUpperBodyLayerWeight(0f);
    }
}
