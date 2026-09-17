using UnityEngine;

public sealed class DialogueInteractable : AbstractInteractable
{
    [SerializeField] private int layerOnFocus = 8;
    [SerializeField] private Fabler fabler;
    [SerializeField] private Transform cameraPivot;

    [SerializeField] private bool activateCursor;
    [SerializeField] public AbstractDialogueEntry entry;

    private PlayerController Player;
    private CameraController activeCameraController;
    private Transform oldCameraPivot;
    private AbstractCameraState oldCameraState;

    private void Awake()
    {
        CacheDefaultLayers();
    }




    public override void OnEnter(PlayerController player)
    {   
        Debug.Log($"DialogueInteractable : start interaction with {this.gameObject}");
        Player = player;
        ApplyAndBackupDialogueCamera(player);


        if (!TryStartDialogue())
        {
            Player.SetState(Player.IdleState);
        }
    }

    public override bool AllowsBreak()
    {
        return false;
    }

    public override void OnExit()
    {
        RestoreDialogueCamera();

    }

    public override void KillInteraction()
    {
        RestoreDialogueCamera();
        Player.SetState(Player.IdleState);
        Player = null;
        Destroy(this);
    }

    public bool TryStartDialogue(Fabler dialogueController = null)
    {
        if (entry == null)
        {
            Debug.LogWarning($"{nameof(DialogueInteractable)} on {gameObject.name} has no {nameof(DialogueEntry)} assigned.");
            return false;
        }

        dialogueController ??= GetFabler();
        if (dialogueController == null)
        {
            Debug.LogWarning($"{nameof(DialogueInteractable)} on {gameObject.name} could not find a {nameof(Fabler)}.");
            return false;
        }

        dialogueController.StartDialogue(entry);
        return true;
    }

    private Fabler GetFabler()
    {
        if (fabler == null)
        {
            fabler = FindAnyObjectByType<Fabler>();
        }

        return fabler;
    }

    private void ApplyAndBackupDialogueCamera(PlayerController player)
    {
        if (cameraPivot == null || player == null || player.PlayerCameraController == null)
        {
            return;
        }

        activeCameraController = player.PlayerCameraController;
        oldCameraPivot = activeCameraController.CameraPivotTransform;
        oldCameraState = activeCameraController.CurrentState;

        activeCameraController.SetCameraPivot(cameraPivot);
        activeCameraController.SetState(activeCameraController.RestrictedLookAtCameraState);

        MouseVisibilityController.Instance.BackupCursor();
        if (activateCursor)
        {            
            MouseVisibilityController.Instance.ActivateCursor();
        }
    }

    private void RestoreDialogueCamera()
    {
        if (activeCameraController == null)
        {
            return;
        }

        activeCameraController.SetCameraPivot(oldCameraPivot);
        activeCameraController.SetState(oldCameraState);

        activeCameraController = null;
        oldCameraPivot = null;
        oldCameraState = null;
        MouseVisibilityController.Instance.RestoreCursor();
        Player = null;
    }

    public override void OnFocusEnter()
    {
        ApplyLayerToHierarchy(layerOnFocus);
    }

    public override void OnFocusExit()
    {
        RestoreDefaultLayers();
    }

}
