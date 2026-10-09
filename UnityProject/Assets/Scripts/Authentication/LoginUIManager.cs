using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using AIBusinessTycoon.Authentication;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// Controls the Login / Register screen.
    /// Mirrors the architecture of LoginUIController from MysteryRooms.
    /// Wire all UI references via Inspector.
    /// </summary>
    public class LoginUIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Input Fields")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button         togglePasswordVisibility;
        [SerializeField] private Image          passwordVisibilityIcon;
        [SerializeField] private Sprite         eyeOpenSprite;
        [SerializeField] private Sprite         eyeClosedSprite;

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button guestButton;
        [SerializeField] private Button toggleModeButton;

        [Header("Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text toggleModeText;
        [SerializeField] private TMP_Text loadingText;

        [Header("Toast")]
        [SerializeField] private GameObject errorToast;
        [SerializeField] private TMP_Text   errorToastText;
        [SerializeField] private float      toastDuration = 3f;

        [Header("Scene")]
        [SerializeField] private string gameSceneName = "Game";

        private bool isRegisterMode  = false;
        private bool isPasswordVisible = false;

        private void Start()
        {
            if (TycoonAuthService.HasSavedSession())
            {
                Debug.Log("[LoginUIManager] Saved session found. Loading game directly.");
                LoadGame();
                return;
            }

            loginButton.onClick.AddListener(OnLoginClicked);
            registerButton.onClick.AddListener(OnRegisterClicked);
            guestButton.onClick.AddListener(OnGuestClicked);
            toggleModeButton.onClick.AddListener(ToggleMode);
            if (togglePasswordVisibility != null)
                togglePasswordVisibility.onClick.AddListener(TogglePasswordVisibility);

            SetLoginMode();
            if (errorToast   != null) errorToast.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        private void ToggleMode()
        {
            isRegisterMode = !isRegisterMode;
            if (isRegisterMode) SetRegisterMode(); else SetLoginMode();
        }

        private void SetLoginMode()
        {
            isRegisterMode = false;
            if (titleText      != null) titleText.text      = "Welcome Back!";
            if (toggleModeText != null) toggleModeText.text = "New here? Register";
            if (nameInput      != null) nameInput.gameObject.SetActive(false);
            if (loginButton    != null) loginButton.gameObject.SetActive(true);
            if (registerButton != null) registerButton.gameObject.SetActive(false);
            if (statusText     != null) statusText.text     = string.Empty;
        }

        private void SetRegisterMode()
        {
            isRegisterMode = true;
            if (titleText      != null) titleText.text      = "Create Account";
            if (toggleModeText != null) toggleModeText.text = "Already have an account? Login";
            if (nameInput      != null) nameInput.gameObject.SetActive(true);
            if (loginButton    != null) loginButton.gameObject.SetActive(false);
            if (registerButton != null) registerButton.gameObject.SetActive(true);
            if (statusText     != null) statusText.text     = string.Empty;
        }

        private void OnLoginClicked()
        {
            string email = emailInput    != null ? emailInput.text.Trim() : "";
            string pass  = passwordInput != null ? passwordInput.text     : "";
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            { ShowToast("Please enter your email and password."); return; }

            ShowLoading("Logging in...");
            TycoonAuthService.Instance.Login(email, pass,
                (r) => { TycoonAuthService.SaveSession(r); HideLoading(); LoadGame(); },
                (e) => { HideLoading(); ShowToast(e); }
            );
        }
        private void OnRegisterClicked()
        {
            string name  = nameInput     != null ? nameInput.text.Trim()  : "";
            string email = emailInput    != null ? emailInput.text.Trim() : "";
            string pass  = passwordInput != null ? passwordInput.text     : "";

            if (string.IsNullOrEmpty(name))  { ShowToast("Please enter your name."); return; }
            if (string.IsNullOrEmpty(email)) { ShowToast("Please enter your email."); return; }
            if (pass.Length < 6)             { ShowToast("Password must be at least 6 characters."); return; }

            ShowLoading("Creating account...");
            TycoonAuthService.Instance.Register(name, email, pass,
                (r) => { TycoonAuthService.SaveSession(r); HideLoading(); LoadGame(); },
                (e) => { HideLoading(); ShowToast(e); }
            );
        }

        private void OnGuestClicked()
        {
            ShowLoading("Entering as Guest...");
            string guestName = $"Tycoon {UnityEngine.Random.Range(1000, 9999)}";
            TycoonAuthService.Instance.LoginGuest(guestName,
                (r) => { TycoonAuthService.SaveSession(r); HideLoading(); LoadGame(); },
                (e) => { HideLoading(); ShowToast(e); }
            );
        }

        private void TogglePasswordVisibility()
        {
            isPasswordVisible = !isPasswordVisible;
            if (passwordInput != null)
            {
                passwordInput.contentType = isPasswordVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                passwordInput.ForceLabelUpdate();
            }
            if (passwordVisibilityIcon != null && eyeOpenSprite != null && eyeClosedSprite != null)
                passwordVisibilityIcon.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
        }

        private void ShowLoading(string message = "Loading...")
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);
            if (loginPanel   != null) loginPanel.SetActive(false);
            if (loadingText  != null) loadingText.text = message;
        }

        private void HideLoading()
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (loginPanel   != null) loginPanel.SetActive(true);
        }

        private void ShowToast(string message)
        {
            if (errorToast != null)
            {
                if (errorToastText != null) errorToastText.text = message;
                errorToast.SetActive(true);
                StopCoroutine(nameof(HideToastAfterDelay));
                StartCoroutine(nameof(HideToastAfterDelay));
            }
            if (statusText != null) statusText.text = message;
            Debug.LogWarning($"[LoginUIManager] {message}");
        }

        private System.Collections.IEnumerator HideToastAfterDelay()
        {
            yield return new WaitForSeconds(toastDuration);
            if (errorToast != null) errorToast.SetActive(false);
        }

        private void LoadGame() => SceneManager.LoadScene(gameSceneName);
    }
}
