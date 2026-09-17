using UnityEngine;

public class FocusInteractable : AbstractInteractable
{
    [SerializeField] private int layerOnFocus = 8;
    [SerializeField] private Transform cameraPivot;

    [SerializeField] private bool activateCursor;

    [SerializeField] private CameraController.CameraStateKind focusCameraState;

    public Transform CameraPosition;
    //private bool OldCursorVisible;
    //private CursorLockMode OldCursorState;
    private bool suppressFocusLayerApplication;
    private bool isFocused;

    

    private PlayerController Player;
    private CameraController activeCameraController;
    private Transform oldCameraPivot;
    private AbstractCameraState oldCameraState;

    private void Awake()
    {   
        suppressFocusLayerApplication = false;
        CacheDefaultLayers();
    }

    public override void OnEnter(PlayerController player)
    {
        Player = player;
        suppressFocusLayerApplication = true;
        RestoreDefaultLayers();
        //OldCursorVisible = Cursor.visible;
        //OldCursorState = Cursor.lockState;
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
        ApplyAndBackupFocusCamera(player);
        //ObjectAligner.CameraAligner.SetTarget(CameraPosition);
    }

    public override void OnExit()
    {
        //Cursor.visible = OldCursorVisible;
        //Cursor.lockState = OldCursorState;
        RestoreFocusCamera();

        suppressFocusLayerApplication = false;
        Player = null;

        if (isFocused)
        {
            ApplyLayerToHierarchy(layerOnFocus);
        }
        else
        {
            RestoreDefaultLayers();
        }
    }

    public override void KillInteraction()
    {
        var player = Player;
        RestoreFocusCamera();

        if (player != null)
        {
            player.SetState(player.IdleState);
        }

        Player = null;
        Destroy(this);
    }

    private void ApplyAndBackupFocusCamera(PlayerController player)
    {
        if (cameraPivot == null || player == null || player.PlayerCameraController == null)
        {
            return;
        }

        activeCameraController = player.PlayerCameraController;
        oldCameraPivot = activeCameraController.CameraPivotTransform;
        oldCameraState = activeCameraController.CurrentState;

        activeCameraController.SetCameraPivot(cameraPivot);
        activeCameraController.SetState(focusCameraState);

        MouseVisibilityController.Instance.BackupCursor();
        if (activateCursor)
        {            
            MouseVisibilityController.Instance.ActivateCursor();
        }
    }

    private void RestoreFocusCamera()
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
    }

    public override bool AllowsBreak()
    {
        return true;
    }

    public override void OnFocusEnter()
    {
        isFocused = true;

        if (suppressFocusLayerApplication)
        {
            return;
        }
        ApplyLayerToHierarchy(layerOnFocus);
    }

    public override void OnFocusExit()
    {
        isFocused = false;
        RestoreDefaultLayers();
    }

    public void CompleteInteraction()
    {
        if (Player != null)
        {
            Player.SetState(Player.IdleState);
        }
    }

}
