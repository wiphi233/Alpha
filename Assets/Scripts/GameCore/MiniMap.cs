using System;
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MiniMap : MonoBehaviour
{
    [Header("Other GameObject References 其他游戏对象引用")]
    public GameObject Player;
    public GameObject MainCamera;
    public GameObject MiniMapObject;
    public GameObject PlayerArrow;
    public GameObject PositionTextObject;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    private ISignalCenter signalCenter;

    private TextMeshProUGUI PositionText;
    private Camera miniCamera;
    private RenderTexture targetRT;
    private const float updateInterval = 1f / 20f;


    void Start()
    {
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        MiniMapObject.SetActive(false);
        signalCenter.Subscribe(SignalType.WorldLoaded, OnWorldLoaded);
        miniCamera = GetComponent<Camera>();
        targetRT = miniCamera.targetTexture;
        miniCamera.enabled = false;
        PositionText = PositionTextObject.GetComponent<TextMeshProUGUI>();
    }

    public void OnWorldLoaded(GameObject sender, object data)
    {
        MiniMapObject.SetActive(true);
        StartCoroutine(LowFreqMiniMapUpdate());
    }

    IEnumerator LowFreqMiniMapUpdate()
    {
        var wait = new WaitForSeconds(updateInterval);
        while (true)
        {
            yield return wait;

            transform.position = new Vector3(
                Player.transform.position.x,
                transform.position.y,
                Player.transform.position.z
            );

            PlayerArrow.transform.rotation = Quaternion.Euler(0, 0, -Player.transform.eulerAngles.y);
            if (targetRT != null)
            {
                miniCamera.Render();
            }
            PositionText.SetText($"x:{Math.Round(Player.transform.position.x, 1)} y:{Math.Round(Player.transform.position.y, 1)} z:{Math.Round(Player.transform.position.z, 1)}");
        }
    }
}