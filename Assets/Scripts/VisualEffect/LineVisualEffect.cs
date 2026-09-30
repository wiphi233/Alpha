using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using NaughtyAttributes;

public class LineVisualEffect : MonoBehaviour
{
    [Dropdown("StringValues")]
    public string stringValue;

    public int LineLength = 20;
    public int Number = 10;
    public int Radius = 5;
    private VisualEffect visualEffect;

    private List<string> StringValues { get { return new List<string>() { "Line", "Circle" }; } }

    private void Awake()
    {
        visualEffect = GetComponentInParent<VisualEffect>();
    }

    void Start()
    {

    }

    [Button]
    public void OnPlayVisualEffect()
    {
        if (stringValue == "Circle")
        {
            for (float i = 0f; i <= Mathf.PI; i += Mathf.PI / Number)
            {
                float x = Mathf.Cos(i) * Radius;
                float y = Mathf.Sin(i) * Radius;
                visualEffect.SetVector3("PositionOffset", new Vector3(x, 0, y));
                visualEffect.SendEvent("OnPlay");
            }
        }
        else if (stringValue == "Line")
        {
            int move = LineLength / Number;
            for (int zOffset = -LineLength / 2; zOffset <= LineLength / 2; zOffset += move)
            {
                visualEffect.SetVector3("PositionOffset", new Vector3(0, 0, zOffset));
                visualEffect.SendEvent("OnPlay");
            }
        }
    }

    void Update()
    {
        
    }
}
