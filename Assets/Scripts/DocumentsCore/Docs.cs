using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class Docs : MonoBehaviour
{
    public GameObject Book;
    public GameObject BookText;
    public GameObject DocsCanvas;
    public GameObject DocIcon;
    public GameObject ScrollView;
    public GameObject Content;
    public GameObject Title;
    public GameObject ExitButton;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    public GameObject dataManagerObject;
    private ISignalCenter signalCenter;
    private IDataManager dataManager;

    private bool AboutUsCheckBook = false;
    private AudioSource FlippingPagesSource;

    void Awake()
    {
        if (Book == null || BookText == null || DocsCanvas == null || DocIcon == null ||
            ScrollView == null || Content == null || Title == null || ExitButton == null)
        {
            Debug.LogError("请在Inspector中为Docs脚本分配所有必要的引用！");
        }
        dataManager = dataManagerObject.GetComponent<IDataManager>();
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
    }

    public void OnAboutUsClicked()
    {
        AboutUsCheckBook = true;
        signalCenter.Emit(SignalType.ExitHomepage, gameObject);
        //signalCenter.Emit(SignalType.ChangeAngularBall, gameObject, AngularBallState.Inactive);

        Book.SetActive(true);
        BookText.SetActive(true);
    }

    void Update()
    {
        // 检查鼠标左键是否刚按下（相当于 Input.GetMouseButtonDown(0)）
        if (AboutUsCheckBook && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 获取鼠标位置：Mouse.current.position.ReadValue()
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == Book)
                {
                    Book.SetActive(false);
                    BookText.SetActive(false);
                    AboutUsCheckBook = false;
                    //FlippingPagesSource = AudioManager.Instance.PlayAudio(AudioManager.Instance.flippingPages);
                    FlippingPagesSource = AudioManager.Instance.PlayAudio(AudioManager.Instance.ButtonClick);
                    DocsCanvas.SetActive(true);
                    TextMeshProUGUI tmp = Title.GetComponent<TextMeshProUGUI>();
                    tmp.SetText("关于我们 About Us");
                    tmp = Content.GetComponent<TextMeshProUGUI>();

                    dataManager.LoadData<string>("Documents/AboutUs", (loadedData) =>
                    {
                        if (loadedData != null)
                        {
                            tmp.SetText(loadedData);
                        }
                        else
                        {
                            Debug.LogError($"文档数据加载失败：AboutUs");
                        }
                    }, DataFormat.TEXT);

                    // 滚动顶部
                    ScrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
                }
            }
        }
    }

    public void OnExitButtonClicked()
    {
        FlippingPagesSource?.Stop();
        DocsCanvas.SetActive(false);
        signalCenter.Emit(SignalType.BootCompletion, gameObject);
        //signalCenter.Emit(SignalType.ChangeAngularBall, gameObject, AngularBallState.Active);
    }
}
