using UnityEngine;
using UnityEditor;

public class DiagnosticRigidbody
{
    [UnityEditor.MenuItem("HumanCat/Diagnose Rigidbody")]
    public static void Execute()
    {
        var go = GameObject.Find("MaleHuman");
        if (go == null) { Debug.LogError("[Diag] MaleHuman not found"); return; }

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) { Debug.LogError("[Diag] Rigidbody2D not found"); return; }

        Debug.Log($"[Diag] MaleHuman Rigidbody2D:" +
                  $"\n  bodyType={rb.bodyType}" +
                  $"\n  gravityScale={rb.gravityScale}" +
                  $"\n  simulated={rb.simulated}" +
                  $"\n  constraints={rb.constraints}" +
                  $"\n  velocity={rb.linearVelocity}");

        var col = go.GetComponent<CapsuleCollider2D>();
        Debug.Log($"[Diag] CapsuleCollider2D: {(col != null ? $"size={col.size} isTrigger={col.isTrigger}" : "MISSING")}");
    }
}
