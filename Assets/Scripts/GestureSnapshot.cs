using Leap;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Leap.Unity;
using System.Linq;

[RequireComponent(typeof(ToggleableObject))]
public class GestureSnapshot : MonoBehaviour
{
    [Multiline]
    [SerializeField]
    private string header;

    [SerializeField]
    public Text controlsText;

    [SerializeField]
    private InputField gestureInputField;

    [SerializeField]
    private Text gestureStatusText;

    private Text gestureInputFieldPlaceHolderText;

    [SerializeField]
    private DataService dataService;

    [SerializeField]
    private Button submitGestureButton;

    [SerializeField]
    private KeyCode takeSnapShotKey = KeyCode.S;

    [SerializeField]
    private KeyCode resetSnapshotKey = KeyCode.R;

    [SerializeField]
    private GameObject leapController;

    private LeapServiceProvider leapControllerProvider;

    private List<Frame> gestureFrames;

    public string GestureInputText { get { return gestureInputField.text; } set { gestureInputField.text = value; } }
    public bool GestureInputInteractable { get { return gestureInputField.interactable; } set { gestureInputField.interactable = value; } }
    public bool GestureSubmitButtonInteractable { get { return submitGestureButton.interactable; } set { submitGestureButton.interactable = value; } }
    public string GestureStatusText { get { return gestureStatusText.text; } set { gestureStatusText.text = value; } }

    private bool lastActiveViewState;
    private bool isGatheringFrames;
    private float recordingTimer;

    [SerializeField]
    [Range(1.0f, 5.0f)]
    private float recordingTime;

    [SerializeField]
    private ModalDialog errorModalDialog;

    [SerializeField]
    private ModalDialog resetModalDialog;

    private ToggleableObject toggleableObject;

    private AudioSource buttonAudioSource;

    [SerializeField]
    private AudioClip buttonClickSound;

    [SerializeField]
    private GameObject processingFramesBackground;

    [SerializeField]
    private GameObject loadingCircle;

    [SerializeField]
    private Text processingFramesText;

    private void Start()
    {
        leapControllerProvider = leapController.GetComponent<LeapServiceProvider>();

        gestureInputFieldPlaceHolderText = gestureInputField.placeholder.GetComponent<Text>();
        buttonAudioSource = GetComponent<AudioSource>();

        toggleableObject = GetComponent<ToggleableObject>();
        lastActiveViewState = true;

        isGatheringFrames = false;
        recordingTimer = recordingTime;

        gestureFrames = new List<Frame>();

        if (controlsText != null)
        {
            controlsText.text = header + "\n\n" + takeSnapShotKey +
                " - Take a snapshot\n" + toggleableObject.toggleKey + " - Toggle view\n" +
                resetSnapshotKey + " - Reset Snapshot taken";

        }
    }

    private void Update()
    {
        if(isGatheringFrames && recordingTimer > 0)
            gatherGestureFrames();

        if (!GestureInputInteractable && !isGatheringFrames && Input.GetKeyDown(takeSnapShotKey))
        {
            isGatheringFrames = true;

            lastActiveViewState = false;
            toggleableObject.toggleObject(lastActiveViewState);
        }

        if (!gestureInputField.isFocused && Input.GetKeyDown(toggleableObject.toggleKey))
        {
            lastActiveViewState = !lastActiveViewState;
            toggleableObject.toggleObject(lastActiveViewState);
        }

        if (Input.GetKeyDown(resetSnapshotKey) && !gestureInputField.isFocused &&
           !errorModalDialog.isDialogActive() && gestureFrames.Count != 0)
        {
            resetModalDialog.showQuestionDialog(YesResetGestureEvent, NoResetGestureEvent);
        }

        if (gestureInputField.isFocused)
            gestureInputFieldPlaceHolderText.text = "";
        else
            gestureInputFieldPlaceHolderText.text = "Enter Gesture Name...";
    }

    private void gatherGestureFrames()
    {
        Frame frame = leapControllerProvider.GetLeapController().Frame();

        if (frame.Hands.Count > 0)
        {
            gestureFrames.Add(frame);
        }

        recordingTimer -= Time.deltaTime;

        if (recordingTimer < 0)
        {
            isGatheringFrames = false;
            recordingTimer = recordingTime;
            GestureInputInteractable = true;
            GestureSubmitButtonInteractable = true;
            lastActiveViewState = true;
            toggleableObject.toggleObject(lastActiveViewState);
            GestureStatusText = "Process complete. Enter a name for the gesture.";
            StartCoroutine(resetText());
        }
    }

    private IEnumerator createGesture(string gestureName)
    {
        processingFramesBackground.SetActive(true);
        loadingCircle.SetActive(true);
        processingFramesText.text = "Creating Gesture...";

        Thread gestureThread = new Thread(() => processGestureFrames(gestureName));
        gestureThread.Start();

        while(gestureFrames.Count > 0)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1.0f);

        loadingCircle.SetActive(false);
        processingFramesText.text = "Gesture Created";

        yield return new WaitForSeconds(1.0f);

        processingFramesBackground.SetActive(false);

        GestureInputInteractable = false;
        GestureSubmitButtonInteractable = false;
    }

    public void sendGestureToDatabase()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);

        string gestureName = GestureInputText.Trim();

        if (!string.IsNullOrEmpty(gestureName))
        {
            StartCoroutine(createGesture(gestureName));
        }
        else
            errorModalDialog.showErrorDialog(OkEvent);

        GestureInputText = "";
    }

    private void processGestureFrames(string gestureName)
    {
        int gestureClassLabel = dataService.gestureToClassLabel(gestureName);

        FeatureVectorPreprocessor featureVectorPreProcessor = new FeatureVectorPreprocessor();
        List<FeatureVector> featureVectors = new List<FeatureVector>();

        foreach (Frame gestureFrame in gestureFrames)
        {
            FeatureVector featureVector = featureVectorPreProcessor.createFeatureVector(gestureFrame);
            featureVector.GestureClassLabel = gestureClassLabel;
            featureVector.Gesture = gestureName;

            featureVectors.Add(featureVector);
        }

        dataService.InsertFeatureVectors(featureVectors);

        gestureFrames.Clear();
    }

    private IEnumerator resetText()
    {
        yield return new WaitForSeconds(3);
        GestureStatusText = "";
    }

    public void OkEvent()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);
    }

    public void YesResetGestureEvent()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);
        GestureStatusText = "Gesture discarded. Ready for a new gesture.";
        gestureFrames.Clear();
        GestureInputInteractable = false;
        GestureSubmitButtonInteractable = false;
        StartCoroutine(resetText());
    }

    public void NoResetGestureEvent()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);
    }
}