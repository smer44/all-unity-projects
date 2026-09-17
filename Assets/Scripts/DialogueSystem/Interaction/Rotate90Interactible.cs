using System.Collections;
using UnityEngine;

public class Rotate90Interactible : AbstractInteractable
{
    private const float TargetRotationDegrees = 90f;

    [SerializeField] private int layerOnFocus = 8;
    [SerializeField] private Vector3 rotationAxis = new Vector3(0f, 1f, 0f);
    [SerializeField, Min(0.01f)] private float rotationDuration = 1.5f;
    [SerializeField, Min(0f)] private float exitDelay = 0.25f;
    [SerializeField, Range(0, 3)] private int rotationStep;

    private Coroutine rotationRoutine;
    private bool isCompletingInteraction;
    private PlayerController Player;
    private Quaternion zeroStepLocalRotation;

    private void Awake()
    {
        CacheDefaultLayers();
        Vector3 axis = GetRotationAxis();
        zeroStepLocalRotation = transform.localRotation
            * Quaternion.Inverse(Quaternion.AngleAxis(rotationStep * TargetRotationDegrees, axis));
    }

    public override void OnEnter(PlayerController player)
    {
        Player = player;
        StopActiveRoutine();
        rotationRoutine = StartCoroutine(RotateAndExit());
    }

    public override void OnExit()
    {
        if (!isCompletingInteraction)
        {
            StopActiveRoutine();
        }

        isCompletingInteraction = false;
        Player = null;
    }

    public override void KillInteraction()
    {
        var player = Player;

        StopActiveRoutine();
        isCompletingInteraction = false;
        Player = null;

        if (player != null)
        {
            player.SetState(player.IdleState);
        }

        Destroy(this);
    }

    public override bool AllowsBreak()
    {
        return false;
    }

    public override void OnFocusEnter()
    {
        ApplyLayerToHierarchy(layerOnFocus);
    }

    public override void OnFocusExit()
    {
        RestoreDefaultLayers();
    }

    private IEnumerator RotateAndExit()
    {
        Vector3 axis = GetRotationAxis();
        float startAngle = rotationStep * TargetRotationDegrees;
        float targetAngle = startAngle + TargetRotationDegrees;
        int nextStep = (rotationStep + 1) % 4;
        float rotationSpeed = TargetRotationDegrees / Mathf.Max(rotationDuration, 0.01f);
        Quaternion startRotation = GetRotationForAngle(startAngle, axis);
        Quaternion targetRotation = GetRotationForAngle(targetAngle, axis);

        transform.localRotation = startRotation;

        while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.001f)
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localRotation = targetRotation;
        rotationStep = nextStep;

        if (exitDelay > 0f)
        {
            yield return new WaitForSeconds(exitDelay);
        }

        if (Player != null)
        {
            var player = Player;
            isCompletingInteraction = true;
            player.SetState(player.IdleState);
        }

        rotationRoutine = null;
        isCompletingInteraction = false;
    }

    private Quaternion GetRotationForAngle(float angle, Vector3 axis)
    {
        return zeroStepLocalRotation * Quaternion.AngleAxis(angle, axis);
    }

    private Vector3 GetRotationAxis()
    {
        if (rotationAxis.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.forward;
        }

        return rotationAxis.normalized;
    }


    private void StopActiveRoutine()
    {
        if (rotationRoutine == null)
        {
            return;
        }

        StopCoroutine(rotationRoutine);
        rotationRoutine = null;
    }
}
