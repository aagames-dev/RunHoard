using UnityEngine;

public static class TriggerUtils
{
    // Crowd stickmen and the player leader are compound colliders (Glow/Body children plus the root
    // capsule), so the collider that actually touches a trigger is often a child, not the GameObject
    // the relevant script lives on - GetComponentInParent is required here, or hits get silently missed.
    public static StickmanController GetStickman(Collider other)
    {
        return other.GetComponentInParent<StickmanController>();
    }

    public static PlayerController GetLeader(Collider other)
    {
        return other.GetComponentInParent<PlayerController>();
    }

    // Disables every collider on the target immediately (synchronous) rather than relying only on
    // Destroy(), which doesn't take effect until end of frame. Each stickman is a compound collider
    // (Glow/Body children plus the root capsule), so multiple of them can independently overlap the
    // same trigger in one physics step - disabling just one collider would leave the rest free to each
    // fire their own trigger logic before the object is actually removed.
    public static void DisableAllColliders(Component target)
    {
        foreach (Collider c in target.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
    }
}
