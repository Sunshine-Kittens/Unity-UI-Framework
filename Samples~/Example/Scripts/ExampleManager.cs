using UnityEngine;
using UIFramework;

using UnityEngine.Extension;

public class ExampleManager : MonoBehaviour, IUpdatable
{
    [SerializeField] private ExampleController exampleController = null;

    public bool Active => gameObject.activeInHierarchy;
    
    private void Awake()
    {        
        UpdateManager.AddUpdatable(this);
    }

    private void OnEnable()
    {
        BehaviourState controllerState = exampleController.State;
        exampleController.Initialize();
        if(controllerState == BehaviourState.Terminated)
        {
            exampleController.OpenScreen<UGUIExampleTransitionScreen>();
        }
    }

    private void OnDisable()
    {
        exampleController.Terminate();
    }

    private void Start()
    {
        exampleController.OpenScreen<UGUIExampleTransitionScreen>();
    }

    public void ManagedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!exampleController.IsOpen)
            {
                exampleController.OpenScreen<UGUIExampleTransitionScreen>(new AccessAnimationParams(GenericWindowAnimationType.Fade, 0.5F, EasingMode.EaseInOut));
            }
        }
    }

    private void OnDestroy()
    {
        UpdateManager.RemoveUpdatable(this);
    }        
}
