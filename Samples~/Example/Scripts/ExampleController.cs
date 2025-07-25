using System;

using UIFramework;
using UIFramework.UGUI;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.UI;

public class ExampleController : Controller
{
    public RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
            return _rectTransform;
        }
    }
    private RectTransform _rectTransform = null;

    public CanvasGroup canvasGroup
    {
        get
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            return _canvasGroup;
        }
    }
    private CanvasGroup _canvasGroup = null;

    [SerializeField] private Button backButton = null;

    public override TimeMode TimeMode { get; protected set; } = TimeMode.Scaled;

    protected override AccessAnimationPlayable CreateAccessPlayable(AccessOperation accessOperation, float length)
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        UGUIGenericWindowAnimation animation =
            new UGUIGenericWindowAnimation(canvas.transform as RectTransform, rectTransform, Vector3.zero, canvasGroup, GenericWindowAnimationType.Fade);
        return animation.CreatePlayable(accessOperation, EasingMode.EaseInOut, TimeMode);
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        gameObject.SetActive(false);
        backButton.onClick.AddListener(CloseScreenAction);
    }

    protected override void OnTerminate()
    {
        base.OnTerminate();
        backButton.onClick.RemoveListener(CloseScreenAction);
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!TryCloseScreen())
            {
                CloseAll(0.5F);
            }
        }
    }

    public override void SetWaiting(bool waiting)
    {
        throw new NotImplementedException();
    }

    protected override void SetBackButtonActive(bool active)
    {
        if (ActiveScreen != null && ActiveScreen.SetBackButtonActive(active))
        {
            backButton?.gameObject.SetActive(false);
        }
        else
        {
            backButton?.gameObject.SetActive(active);
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        gameObject.SetActive(true);
    }

    protected override void OnClosed()
    {
        base.OnClosed();
        gameObject.SetActive(false);
    }

    private void CloseScreenAction()
    {
        TryCloseScreen();
    }
}
