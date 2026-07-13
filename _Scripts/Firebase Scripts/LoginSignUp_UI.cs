using System;
using System.Collections;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
//using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using WebSocketSharp;

public class LoginSignUp_UI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject signUpPanel;

    [Header("Login Fields")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;

    [Header("Sign Up Fields")]
    public TMP_InputField signUpEmailInput;
    public TMP_InputField signUpPasswordInput;
    public TMP_InputField usernameInput;
    public GameObject warningPanel;
    private AuthManager authManager;

    void Start()
    {
        authManager = FindFirstObjectByType<AuthManager>();
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
        warningPanel.SetActive(false);
    }

    public void ActivateSignUpPanel()
    {
        loginPanel.SetActive(false);
        signUpPanel.SetActive(true);
    }

    public void ActivateLoginPanel()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }

    public void OnLoginButton()
    {
        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;
        authManager.LoginUser(email, password);
    }

    public void OnLogoutButton()
    {
        authManager.Logout();
    }

    public void OnSignUpButton()
    {
        string email = signUpEmailInput.text;
        string password = signUpPasswordInput.text;
        string username = usernameInput.text;
        if (email.IsNullOrEmpty() || password.IsNullOrEmpty() || username.IsNullOrEmpty())
        {
            StartCoroutine(Warning("Enter your credentials first."));
            return;
        }
        authManager.RegisterUser(email, password, username);
        ActivateLoginPanel();
    }

    private IEnumerator Warning(string warning)
    {
        var warningText = warningPanel.GetComponentInChildren<TextMeshProUGUI>();
        warningPanel.SetActive(true);
        warningText.text = warning;

        yield return new WaitForSeconds(3f);

        warningPanel.SetActive(false);
        warningText.text = "";
    }

    public void Guest()
    {
        SceneManager.LoadScene("Character Selection");
    }

}
