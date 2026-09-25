using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(25)]
[AddComponentMenu("Animation/Player Visuals Shooting Tweaks")]
public sealed class PlayerVisualsShootingTweaks : MonoBehaviour
{
    private static readonly int VelocityRightParameter = Animator.StringToHash("VelocityRight");

    [Tooltip("Spine bones ordered from hips to chest. Each receives a progressively larger correction.")]
    [SerializeField] private Transform[] spineBones;
    // Relative to RightStrafeRun's mean hip yaw (76.6 degrees):
    // Run averages -1.4 degrees, and LeftStrafeRun averages -76.7 degrees.
    [SerializeField] private float rightStrafeAngle = 0f;
    [SerializeField] private float straightAngle = 78f;
    [SerializeField] private float leftStrafeAngle = 153f;

    private PlayerController playerController;
    private SpinePose[] spinePoses = System.Array.Empty<SpinePose>();

    private struct SpinePose
    {
        public Transform bone;
        public Quaternion animatedLocalRotation;
        public Quaternion animatedWorldRotation;
        public Quaternion correctedLocalRotation;
    }

    public bool IsOn { get; private set; }
    public float CorrectionAngle { get; private set; }

    private void Awake() => Initialize(GetComponent<PlayerController>());

    public void Initialize(PlayerController owner)
    {
        playerController = owner;
        if ((spineBones == null || spineBones.Length == 0) && owner != null && owner.animator != null)
        {
            Transform spine = owner.animator.transform.Find("mixamorig:Hips/mixamorig:Spine");
            Transform spine1 = spine != null ? spine.Find("mixamorig:Spine1") : null;
            Transform spine2 = spine1 != null ? spine1.Find("mixamorig:Spine2") : null;
            spineBones = new[] { spine, spine1, spine2 };
        }
    }

    public void TurnOn()
    {
        IsOn = true;
        UpdateCorrectionAngle();
    }

    public void TurnOff()
    {
        RemoveCorrection();
        IsOn = false;
        CorrectionAngle = 0f;
    }

    private void Update()
    {
        // Remove the previous frame's offset before animation evaluates again.
        RemoveCorrection();
        if (IsOn)
            UpdateCorrectionAngle();
    }

    private void UpdateCorrectionAngle()
    {
        Animator animator = playerController != null ? playerController.animator : null;
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            CorrectionAngle = 0f;
            return;
        }

        float velocityRight = Mathf.Clamp(animator.GetFloat(VelocityRightParameter), -1f, 1f);
        CorrectionAngle = velocityRight >= 0f
            ? Mathf.Lerp(straightAngle, rightStrafeAngle, velocityRight)
            : Mathf.Lerp(straightAngle, leftStrafeAngle, -velocityRight);
    }

    private void LateUpdate() => ApplySpineCorrection();

    private void ApplySpineCorrection()
    {
        // Apply after the Animator and before UpperBodyVisualsController's arm aiming.
        // Repeated evaluation or a culled Animator must not accumulate the offset.
        RemoveCorrection();
        if (!IsOn || spineBones == null || spineBones.Length == 0 || playerController == null)
            return;

        if (spinePoses.Length != spineBones.Length)
            spinePoses = new SpinePose[spineBones.Length];

        // Capture the whole animated chain before rotating any parent. The fractions
        // describe each bone's total correction, including rotation inherited from its parent.
        for (int i = 0; i < spineBones.Length; i++)
        {
            Transform bone = spineBones[i];
            if (bone == null)
                continue;

            spinePoses[i] = new SpinePose
            {
                bone = bone,
                animatedLocalRotation = bone.localRotation,
                animatedWorldRotation = bone.rotation
            };
        }

        Vector3 up = playerController.visualsPivot != null ? playerController.visualsPivot.up : Vector3.up;
        for (int i = 0; i < spinePoses.Length; i++)
        {
            Transform bone = spinePoses[i].bone;
            if (bone == null)
                continue;

            float angle = CorrectionAngle * (i + 1f) / spineBones.Length;
            bone.rotation = Quaternion.AngleAxis(angle, up) * spinePoses[i].animatedWorldRotation;
            spinePoses[i].correctedLocalRotation = bone.localRotation;
        }
    }

    private void RemoveCorrection()
    {
        // If animation has already written a new pose, keep that pose instead of restoring an old one.
        for (int i = 0; i < spinePoses.Length; i++)
        {
            SpinePose pose = spinePoses[i];
            if (pose.bone != null && pose.bone.localRotation.Equals(pose.correctedLocalRotation))
                pose.bone.localRotation = pose.animatedLocalRotation;
            spinePoses[i].bone = null;
        }
    }

    private void OnDisable() => TurnOff();
}
