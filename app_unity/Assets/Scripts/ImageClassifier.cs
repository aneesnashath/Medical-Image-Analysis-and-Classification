using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using TMPro;
using System.Diagnostics;

public class ImageClassifier : MonoBehaviour
{
    public static ImageClassifier Instance;

    string uploadServerUrl = "http://127.0.0.1:5000/upload"; //Flask server address for upload
    string classifyServerUrl = "http://127.0.0.1:5000/classify"; //Flask server address for classif

    public RawImage ResultImg; //show image in ui
    //public TextMeshProUGUI resultText; // result text

    private Texture2D selectedImage; // user selected image

    private string Path;
    [SerializeField] TextMeshProUGUI[] ResultObj;
    private void Awake()
    {
        Instance = this;

        removetext();
    }

    void hideresult(int no)
    {
        for (int i = 0; i < ResultObj.Length; i++)
        {
            if (no == i)
            {
                ResultObj[no].gameObject.SetActive(true);
            }
            else
            {
                ResultObj[i].gameObject.SetActive(false);
            }
        }
    }

    public void OpenGallery()
    {
        // Open gallery to select an image
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                // Assign the selected image path 
                Path = path;
                print("Path: " + Path);

                // Load the image
                byte[] imageData = File.ReadAllBytes(path);
                selectedImage = new Texture2D(2, 2); // Create a new texture
                selectedImage.LoadImage(imageData); // Load image data into the texture

                // Display the selected image
                ResultImg.texture = selectedImage;

                hideresult(0);
                if (ResultImg.texture != null)
                {
                    ResultObj[0].text = "Image Loaded successfully.";
                    //ResultObj[0].color=UnityEngine.Color.green;
                    manager.INSTANCE.openuplaod();
                }
                else
                {
                    ResultObj[0].text = "Try Again!!";
                    //ResultObj[0].color = UnityEngine.Color.red;
                }

               
            }
        }, "Select Image", "image/*");
    }

    public void UploadImage()
    {
        // Check if an image is selected
        if (Path == null)
        {
            UnityEngine.Debug.LogError("No image selected.");
            return;
        }

        // Load the image data from the selected image
        byte[] imageData = File.ReadAllBytes(Path);

        // Define the cURL command to send an image upload request
        string curlCommand = $"curl -X POST -F \"image=@{Path}\" {uploadServerUrl}";

        // Start the cURL process
        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c {curlCommand}");
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        Process process = Process.Start(psi);
        process.WaitForExit();
        hideresult(1);
        // Check if the process exited without errors
        if (process.ExitCode == 0)
        {
            ResultObj[1].text = "Image sent to the server";
            //ResultObj[1].color = UnityEngine.Color.green;
            print("Image upload request sent successfully.");
            manager.INSTANCE.openclassify();
        }
        else
        {
            ResultObj[1].text = "Try Again!!";
            //ResultObj[1].color = UnityEngine.Color.red;
            UnityEngine.Debug.LogError($"Error: {process.StandardError.ReadToEnd()}");
        }
    }

    public void ClassifyImage()
    {
        // Check if an image is selected
        if (selectedImage == null)
        {
            UnityEngine.Debug.LogError("No image selected.");
            return;
        }

        // Encode the selected image as JPEG
        byte[] imageData = selectedImage.EncodeToJPG();

        // send image data to the Flask server for classification
        StartCoroutine(SendImageToServer(imageData));
    }

    IEnumerator SendImageToServer(byte[] imageData)
    {
        // Create a new form
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageData, "image.jpg", "image/jpeg");

        // Send POST request to Flask server for classification
        using (UnityWebRequest www = UnityWebRequest.Post(classifyServerUrl, form))
        {
            yield return www.SendWebRequest();
            hideresult(2);
            if (www.result == UnityWebRequest.Result.Success)
            {
                //UnityEngine.Debug.Log("Classification result: " + www.downloadHandler.text);
                //print(ExtractPredictionFromServerResponse(www.downloadHandler.text));
                // Display the classification result
                ResultObj[2].text = "Prediction: " + ExtractPredictionFromServerResponse(www.downloadHandler.text);
                print(ResultObj[2].text);
                //ResultObj[2].color = UnityEngine.Color.green;
                manager.INSTANCE.opennext();
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to classify image: " + www.error);
                ResultObj[2].text = "Try Again!!";
                //ResultObj[2].color = UnityEngine.Color.red;
                manager.INSTANCE.opennext();
            }
        }
    }

    public void removetext()
    {
        hideresult(3);
        ResultImg.texture = null;

        //resultText.text = "";
    }

    // extract the prediction value from the server response
    public string ExtractPredictionFromServerResponse(string serverResponse)
    {
        try
        {
            // Parse the JSON string
            var json = JsonUtility.FromJson<ServerResponse>(serverResponse);

            // Return the prediction value
            return json.prediction;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error extracting prediction from server response: " + e.Message);
            return "";
        }
    }

    
    [System.Serializable]
    private class ServerResponse
    {
        public string prediction;
    }
}

