using AwakeComponents.DebugUI;
using UnityEngine;
using extOSC;
using UnityEngine.SceneManagement;

[ComponentInfo("1.0", "22.08.2026")]
public class TabletOSCListener : MonoBehaviour, IDebuggableComponent
{
    [SerializeField] [DebugUIField] private int port = 7001;

    private OSCReceiver receiver;

    public void RenderDebugUI()
    {
        GUILayout.Label("OSCListener");
    }

    private void Awake()
    {

        receiver = gameObject.AddComponent<OSCReceiver>();
        receiver.LocalPort = port;

        receiver.Bind("/1", OnActiveMode);
        receiver.Bind("/2", OnActiveMode);
        receiver.Bind("/3", OnActiveMode);
        receiver.Bind("/stop", OnStop);
        receiver.Bind("/changeLanguage", OnChangeLanguage);
    }

    private void OnActiveMode(OSCMessage message)
    {
        switch (message.Address)
        {
            case "/1":
                SceneManager.LoadScene("Khabarovsk");
                break;
            case "/2":
                SceneManager.LoadScene("Komsomolsk");
                break;
            case "/3":
                SceneManager.LoadScene("Shantari");
                break;
        }
    }

    private void OnStop(OSCMessage message)
    {
        SceneManager.LoadScene("INTRO");
    }

    private void OnChangeLanguage(OSCMessage message)
    {
        Debug.Log("TABLET: CHANGE LANGUAGE");
    }
}