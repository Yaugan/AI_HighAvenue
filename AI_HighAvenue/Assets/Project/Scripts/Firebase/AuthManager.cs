using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;
using System;

public class AuthManager : MonoBehaviour
{
    public TextMeshProUGUI headingText;
    public TextMeshProUGUI descText;

    [Header("Phone Panel")]
    public GameObject phonePanel;
    public TMP_Dropdown countryCodeDropdown;
    public TMP_InputField phoneNumberInput;
    public Button sendOtpButton;
    
    [Header("Keypad")]
    public Button[] numberButtons; // 0-9
    public Button backspaceButton;
    public Button starButton;
    public Button hashButton;

    [Header("Buttons")]
    public Button GoogleBtn;
    public Button AppleBtn;
    public Button PhoneBtn;

    [Header("Model")]
    public GameObject Model;

    // Country data structure
    [System.Serializable]
    public class CountryData
    {
        public string name;
        public string code;
        public string dialCode;
    }

    [System.Serializable]
    public class CountriesAPIResponse
    {
        public List<APICountryData> countries;
    }

    [System.Serializable]
    public class APICountryData
    {
        public CountryName name;
        public string cca2;
        public IDDData idd;
    }

    [System.Serializable]
    public class CountryName
    {
        public string common;
    }

    [System.Serializable]
    public class IDDData
    {
        public string root;
        public string[] suffixes;
    }

    public List<CountryData> countries = new List<CountryData>();
    private string currentPhoneNumber = "";
    private int selectedCountryIndex = 0;

    void Start()
    {
        StartCoroutine(LoadCountriesFromAPI());
        SetupKeypadButtons();
        SetupInputValidation();
        UpdateFormattedDisplay();
        SetupDropdownStyling();
        SetupButtonColors();
    }

    void SetupDropdownStyling()
    {
        if (countryCodeDropdown != null)
        {
            // Add listener to style items when dropdown opens
            countryCodeDropdown.onValueChanged.AddListener((index) => {
                StartCoroutine(StyleDropdownItems());
            });
        }
    }

    void SetupButtonColors()
    {
        if (sendOtpButton != null)
        {
            // Disable the Button component's color tinting
            sendOtpButton.transition = Selectable.Transition.None;
            
            // Set initial button color to black
            Image buttonImage = sendOtpButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.black;
            }
        }
    }
    
    IEnumerator StyleDropdownItems()
    {
        // Wait a frame for the dropdown to open
        yield return null;
        
        // Find all dropdown items and style them
        Transform dropdownList = countryCodeDropdown.template;
        if (dropdownList != null)
        {
            Transform viewport = dropdownList.Find("Viewport");
            if (viewport != null)
            {
                Transform content = viewport.Find("Content");
                if (content != null)
                {
                    // Style each item
                    for (int i = 0; i < content.childCount; i++)
                    {
                        Transform item = content.GetChild(i);
                        if (item != null)
                        {
                            // Increase item height significantly
                            RectTransform itemRect = item.GetComponent<RectTransform>();
                            if (itemRect != null)
                            {
                                itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, 80f); // Much taller items
                            }
                            
                            // Style the text
                            TextMeshProUGUI itemText = item.GetComponentInChildren<TextMeshProUGUI>();
                            if (itemText != null)
                            {
                                itemText.fontSize = 22f; // Much larger font
                                itemText.margin = new Vector4(25f, 20f, 25f, 20f); // Much more padding
                                itemText.alignment = TextAlignmentOptions.Left;
                                

                            }
                        }
                    }
                }
            }
        }
    }

    // Load from external API
    IEnumerator LoadCountriesFromAPI()
    {

        // Using a free countries API - try different endpoint
        string apiUrl = "https://restcountries.com/v3.1/all?fields=name,cca2,flag,idd";
        
        // Alternative API if the first one doesn't work
        // string apiUrl = "https://restcountries.com/v2/all?fields=name,alpha2Code,flag,callingCodes";
        
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                ParseCountriesFromAPI(jsonResponse);
            }
            else
            {
                Debug.LogError($"Failed to load countries: {request.error}");
                LoadDefaultCountries();
            }
        }

        // Initialize dropdown after loading countries
        InitializeCountryDropdown();
    }

    void ParseCountriesFromAPI(string jsonResponse)
    {
        try
        {
            // Debug: Print raw API response
            Debug.Log($"Raw API Response (first 500 chars): {jsonResponse.Substring(0, Mathf.Min(500, jsonResponse.Length))}");
            
            // Parse the REST Countries API response
            CountriesAPIResponse apiResponse = JsonUtility.FromJson<CountriesAPIResponse>("{\"countries\":" + jsonResponse + "}");
            
            countries.Clear();
            
            foreach (var apiCountry in apiResponse.countries)
            {
                if (apiCountry.idd != null && !string.IsNullOrEmpty(apiCountry.idd.root))
                {
                    // Build the complete dial code
                    string completeDialCode = apiCountry.idd.root;
                    
                    // Add suffixes if they exist
                    if (apiCountry.idd.suffixes != null && apiCountry.idd.suffixes.Length > 0)
                    {
                        // For countries with multiple suffixes, use the first one
                        completeDialCode += apiCountry.idd.suffixes[0];
                    }
                    
                    CountryData country = new CountryData
                    {
                        name = apiCountry.name.common,
                        code = apiCountry.cca2,
                        dialCode = completeDialCode
                    };
                    countries.Add(country);
                    
                    // Debug: Print first few countries with all details
                    if (countries.Count <= 5)
                    {
                        Debug.Log($"Parsed: {country.name} | Code: {country.code} | Dial: {country.dialCode}");
                    }
                }
            }

            // Sort countries by name, but keep default countries at top with India first
            var defaultCountryCodes = new[] { "IN", "US", "UK", "CA", "AU" };
            
            // Separate default countries and other countries
            var defaultCountries = countries.Where(c => defaultCountryCodes.Contains(c.code)).ToList();
            var otherCountries = countries.Where(c => !defaultCountryCodes.Contains(c.code)).ToList();
            
            // Sort default countries in specific order (India first)
            defaultCountries.Sort((a, b) => {
                var aIndex = Array.IndexOf(defaultCountryCodes, a.code);
                var bIndex = Array.IndexOf(defaultCountryCodes, b.code);
                return aIndex.CompareTo(bIndex);
            });
            
            // Sort other countries alphabetically
            otherCountries.Sort((a, b) => a.name.CompareTo(b.name));
            
            // Combine: default countries first (India at top), then sorted other countries
            countries.Clear();
            countries.AddRange(defaultCountries);
            countries.AddRange(otherCountries);
            
            Debug.Log($"Loaded {countries.Count} countries from API");
            
            // Debug: Print first few countries to check dial codes
            for (int i = 0; i < Mathf.Min(5, countries.Count); i++)
            {
                Debug.Log($"Final: {countries[i].name} | Dial: {countries[i].dialCode}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing countries: {e.Message}");
            Debug.LogError($"Full error: {e}");
            LoadDefaultCountries();
        }
    }

    void LoadDefaultCountries()
    {
        countries = new List<CountryData>
        {
            new CountryData { name = "India", code = "IN", dialCode = "+91" },
            new CountryData { name = "United States", code = "US", dialCode = "+1" },
            new CountryData { name = "United Kingdom", code = "UK", dialCode = "+44" },
            new CountryData { name = "Canada", code = "CA", dialCode = "+1" },
            new CountryData { name = "Australia", code = "AU", dialCode = "+61" }
        };
    }

    void InitializeCountryDropdown()
    {
        countryCodeDropdown.ClearOptions();
        List<string> options = new List<string>();
        
        foreach (var country in countries)
        {
            // Show only dial code
            options.Add(country.dialCode);
        }
        
        countryCodeDropdown.AddOptions(options);
        countryCodeDropdown.onValueChanged.AddListener(OnCountryChanged);
        
        // Set dropdown properties for better visibility
        if (countryCodeDropdown.template != null)
        {
            // Increase the height of dropdown items
            RectTransform templateRect = countryCodeDropdown.template.GetComponent<RectTransform>();
            if (templateRect != null)
            {
                templateRect.sizeDelta = new Vector2(templateRect.sizeDelta.x, 600f); // Much bigger height
            }
            
            // Find and modify the item template
            Transform itemTemplate = countryCodeDropdown.template.Find("Viewport/Content/Item");
            if (itemTemplate != null)
            {
                RectTransform itemRect = itemTemplate.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    // Increase individual item height significantly
                    itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, 80f); // Much taller items
                }
                
                // Increase text font size
                TextMeshProUGUI itemText = itemTemplate.GetComponentInChildren<TextMeshProUGUI>();
                if (itemText != null)
                {
                    itemText.fontSize = 22f; // Much larger font size
                    itemText.margin = new Vector4(20f, 15f, 20f, 15f); // Much more padding
                }
            }
        }
        
        // Set the dropdown to show more items
        countryCodeDropdown.itemText.fontSize = 22f; // Much larger font size
        countryCodeDropdown.captionText.fontSize = 22f; // Much larger caption font size
    }

    void SetupKeypadButtons()
    {
        // Setup number buttons (0-9)
        for (int i = 0; i < numberButtons.Length && i < 10; i++)
        {
            int number = i;
            numberButtons[i].onClick.AddListener(() => AddDigit(number.ToString()));
        }

        // Setup special buttons
        if (backspaceButton != null)
            backspaceButton.onClick.AddListener(RemoveLastDigit);
        
        if (starButton != null)
            starButton.onClick.AddListener(() => AddDigit("*"));
        
        if (hashButton != null)
            hashButton.onClick.AddListener(() => AddDigit("#"));
    }

    void SetupInputValidation()
    {
        if (phoneNumberInput != null)
        {
            phoneNumberInput.onValueChanged.AddListener(OnPhoneNumberChanged);
            phoneNumberInput.contentType = TMP_InputField.ContentType.Custom;
            phoneNumberInput.characterLimit = 15; // Reasonable limit for phone numbers
        }

        if (sendOtpButton != null)
        {
            sendOtpButton.onClick.AddListener(SendOTP);
            UpdateSendButtonState();
        }
    }

    void OnCountryChanged(int index)
    {
        selectedCountryIndex = index;
        UpdateFormattedDisplay();
    }

    void OnPhoneNumberChanged(string newValue)
    {
        // Remove any non-digit characters except * and #
        currentPhoneNumber = Regex.Replace(newValue, @"[^\d*#]", "");
        UpdateFormattedDisplay();
        UpdateSendButtonState();
    }

    void AddDigit(string digit)
    {
        if (currentPhoneNumber.Length < 15) // Limit phone number length
        {
            currentPhoneNumber += digit;
            UpdateFormattedDisplay();
            UpdateSendButtonState();
        }
    }

    void RemoveLastDigit()
    {
        if (currentPhoneNumber.Length > 0)
        {
            currentPhoneNumber = currentPhoneNumber.Substring(0, currentPhoneNumber.Length - 1);
            UpdateFormattedDisplay();
            UpdateSendButtonState();
        }
    }

    void UpdateFormattedDisplay()
    {
        if (phoneNumberInput != null)
        {
            // Format phone number for better readability
            string formattedNumber = FormatPhoneNumberForDisplay(currentPhoneNumber);
            phoneNumberInput.text = formattedNumber;
        }
    }

    string FormatPhoneNumberForDisplay(string number)
    {
        if (string.IsNullOrEmpty(number))
            return "";

        // Add country code
        string countryCode = countries[selectedCountryIndex].dialCode;
        
        // Format based on length for better readability
        if (number.Length <= 3)
            return $"{countryCode} {number}";
        else if (number.Length <= 6)
            return $"{countryCode} {number.Substring(0, 3)} {number.Substring(3)}";
        else if (number.Length <= 9)
            return $"{countryCode} {number.Substring(0, 3)} {number.Substring(3, 3)} {number.Substring(6)}";
        else
            return $"{countryCode} {number.Substring(0, 3)} {number.Substring(3, 3)} {number.Substring(6, 3)} {number.Substring(9)}";
    }

    void UpdateSendButtonState()
    {
        if (sendOtpButton != null)
        {
            // Enable button if phone number has at least 7 digits
            bool isValid = currentPhoneNumber.Length >= 7 && Regex.IsMatch(currentPhoneNumber, @"^\d+$");
            sendOtpButton.interactable = isValid;
            
            // Get the Image component to control the color directly
            Image buttonImage = sendOtpButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                // Set the button color to black when valid, gray when invalid
                if (isValid)
                {
                    buttonImage.color = Color.black;
                }
                else
                {
                    buttonImage.color = Color.gray;
                }
            }
        }
    }

    void SendOTP()
    {
        if (IsValidPhoneNumber())
        {
            string fullPhoneNumber = countries[selectedCountryIndex].dialCode + currentPhoneNumber;
            Debug.Log($"Sending OTP to: {fullPhoneNumber}");
            
            // TODO: Implement Firebase Phone Authentication here
            // FirebaseAuth.DefaultInstance.VerifyPhoneNumberAsync(fullPhoneNumber, ...)
        }
    }

    bool IsValidPhoneNumber()
    {
        return currentPhoneNumber.Length >= 7 && Regex.IsMatch(currentPhoneNumber, @"^\d+$");
    }

    public void SignInWithPhoneNumber() {
        // Disable text components
        headingText.gameObject.SetActive(false);
        descText.gameObject.SetActive(false);
        
        // Disable buttons
        GoogleBtn.gameObject.SetActive(false);
        AppleBtn.gameObject.SetActive(false);
        PhoneBtn.gameObject.SetActive(false);
        
        // Disable model
        Model.SetActive(false);

        //Activate Phone Panel
        phonePanel.SetActive(true);
    }

    public void BackButton() {
        // Re-enable text components
        headingText.gameObject.SetActive(true);
        descText.gameObject.SetActive(true);
        
        // Re-enable buttons
        GoogleBtn.gameObject.SetActive(true);
        AppleBtn.gameObject.SetActive(true);
        PhoneBtn.gameObject.SetActive(true);
        
        // Re-enable model
        Model.SetActive(true);

        //Deactivate Phone Panel
        phonePanel.SetActive(false);
    }
}
