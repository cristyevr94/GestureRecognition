
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Leap.Unity;

public class UserInterfaceController : MonoBehaviour
{
    private AudioSource buttonAudioSource;

    [SerializeField]
    private AudioClip buttonClickSound;

    [SerializeField]
    private Animator userInterfaceViewAnimator;

    [SerializeField]
    private GameObject leapController;

    private LeapServiceProvider leapControllerProvider;

    [SerializeField]
    private GestureSnapshot snapshotControls;

    [SerializeField]
    private GestureRecording recordingControls;

    [SerializeField]
    private RecordingList recordingList;

    [SerializeField]
    private FreeMode freeMode;

    [SerializeField]
    private GestureClassifier classifier;

    [SerializeField]
    private GameObject trainingClassifierBackground;

    [SerializeField]
    private GameObject loadingCircle;

    [SerializeField]
    private Text trainingStatusText;

    public static readonly string[] animationTriggers = { "GestureSlideOutSnapshotFadeInTrigger",
                                                          "GestureSlideInSnapshotFadeOutTrigger",
                                                          "GestureSlideOutRecordingFadeInTrigger",
                                                          "GestureSlideInRecordingFadeOutTrigger"};

    private void Start()
    {
        leapControllerProvider = leapController.GetComponent<LeapServiceProvider>();
        buttonAudioSource = GetComponent<AudioSource>();
        freeMode.startFreeMode();
        recordingControls.enabled = false;
    }

    public void importGestureClick()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);

        if (!leapControllerProvider.IsConnected())
            return;
    }

    public void recordGestureClick()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);

        if (!leapControllerProvider.IsConnected())
            return;

        userInterfaceViewAnimator.SetTrigger(animationTriggers[2]);
        recordingControls.enabled = true;
        recordingControls.RecordingInputInteractable = false;
        recordingControls.RecordingSubmitButtonInteractable = false;
        recordingList.populateRecordingDropDownList();

        freeMode.stopFreeMode();
    }

    public void createGestureClick()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);

        if (!leapControllerProvider.IsConnected())
            return;

        userInterfaceViewAnimator.SetTrigger(animationTriggers[0]);
        snapshotControls.enabled = true;
        snapshotControls.GestureInputInteractable = false;
        snapshotControls.GestureSubmitButtonInteractable = false;

        freeMode.stopFreeMode();
    }

    public void trainClassifierClick()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);

        if (!leapControllerProvider.IsConnected())
            return;

        StartCoroutine(startTraining());
    }

    private IEnumerator startTraining()
    {
        trainingClassifierBackground.SetActive(true);
        loadingCircle.SetActive(true);
        trainingStatusText.text = "Training Classifier...";

        classifier.ModelExists = false;
        classifier.TrainingFinished = false;

        Thread trainingThread = new Thread(classifier.trainClassifier);
        trainingThread.Start();

        while (!classifier.TrainingFinished)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(0.5f);

        loadingCircle.SetActive(false);
        trainingStatusText.text = "Training Complete";

        yield return new WaitForSeconds(0.5f);

        trainingClassifierBackground.SetActive(false);
        classifier.ModelExists = true;
    }

    public void snapShotViewToGestureView()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);
        userInterfaceViewAnimator.SetTrigger(animationTriggers[1]);

        snapshotControls.GestureInputText = "";
        snapshotControls.GestureStatusText = "";
        snapshotControls.GestureInputInteractable = false;
        snapshotControls.GestureSubmitButtonInteractable = false;
        snapshotControls.enabled = false;

        freeMode.startFreeMode();
        freeMode.GestureSign = "No Gesture Detected";
    }

    public void recordingViewToGestureView()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);
        userInterfaceViewAnimator.SetTrigger(animationTriggers[3]);

        /*
        if (handController.GetLeapRecorder().state != RecorderState.Stopped)
        {
            handController.ResetRecording();
            handController.StopRecording();
        }*/

        recordingControls.RecordingSavedPathText = "";
        recordingControls.RecordingFileInputText = "";
        recordingControls.RecordingInputInteractable = false;
        recordingControls.RecordingSubmitButtonInteractable = false;
        recordingControls.enabled = false;

        freeMode.startFreeMode();
        freeMode.GestureSign = "No Gesture Detected";
    }
}
