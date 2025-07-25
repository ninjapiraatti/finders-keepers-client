using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public InputField serverIPInputField;

    public void StartGame()
    {
        SceneManager.LoadScene("Start");
    }

    public void QuitGame()
    {
        // Quit the application
        Application.Quit();
        Debug.Log("Game is exiting");
    }

    public void SetServerIP()
    {
        string serverIP = serverIPInputField.text;
        Debug.Log("Server IP set to: " + serverIP);
        // Here you can save the server IP for later use
    }
}