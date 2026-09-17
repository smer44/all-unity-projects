using UnityEngine;

public class MouseVisibilityController
{

    public static MouseVisibilityController Instance { get; private set; } = new MouseVisibilityController();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private bool OldCursorVisible;

    private CursorLockMode OldCursorState;

    public MouseVisibilityController()
    {
        BackupCursor();
    }


    public void BackupCursor()
    {
        OldCursorVisible = Cursor.visible;
        OldCursorState = Cursor.lockState;
    }

    public void ActivateCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void DeactivateCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void TogleCursor()
    {
        if (Cursor.visible)
        {
            DeactivateCursor();
            return;
        }

        ActivateCursor();
    }


    public void RestoreCursor()
    {
        Cursor.visible = OldCursorVisible;
        Cursor.lockState = OldCursorState;
    }

}
