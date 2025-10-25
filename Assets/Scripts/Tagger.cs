using System.Linq;
using UnityEngine;

/// <summary>
/// Handles tag detection and input. Press T to tag another player when within radius and in line-of-sight.
/// Uses layer masks to prevent tagging through geometry. On successful tag, awards points via ScoreManager.
/// </summary>
public class Tagger : MonoBehaviour
{
    [Header("Tagging")]
    public float tagRadius = 2.0f;
    public LayerMask playerMask;    // set to layer containing Player colliders
    public LayerMask obstructionMask; // set to environment geometry (walls, terrain)
    public KeyCode tagKey = KeyCode.T;

    [Header("Scoring")]
    public int scorePerTag = 1;

    void Update()
    {
        if (Keyboard.current != null)
        {
            // Using UnityEngine.InputSystem is ok, but fallback to classic KeyCode variable for simplicity in editor:
        }

        if (Input.GetKeyDown(tagKey))
        {
            TryTag();
        }
    }

    /// <summary>
    /// Public method to detect potential targets within radius that satisfy line-of-sight.
    /// Useful for unit tests.
    /// </summary>
    public Collider[] DetectTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, tagRadius, playerMask, QueryTriggerInteraction.Ignore);
        return hits.Where(collider =>
        {
            if (collider.gameObject == gameObject) return false;
            // Check line of sight to collider's center
            Vector3 dir = (collider.bounds.center - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, collider.bounds.center);
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, out RaycastHit hit, dist, obstructionMask | playerMask))
            {
                // If the first hit is the player collider -> good. If it's in obstructionMask -> blocked.
                if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
                {
                    return true;
                }
                return false;
            }
            // No obstruction and no hits - allow if within radius
            return true;
        }).ToArray();
    }

    private void TryTag()
    {
        var targets = DetectTargets();
        if (targets.Length == 0) return;

        // Tag the closest valid target
        Collider closest = targets.OrderBy(t => Vector3.Distance(transform.position, t.bounds.center)).FirstOrDefault();
        if (closest != null)
        {
            var tagged = closest.GetComponent<TaggedState>();
            if (tagged != null)
            {
                tagged.OnTagged(gameObject);
                Debug.Log($"{gameObject.name} tagged {closest.gameObject.name}");

                // Award score to the tagger via ScoreManager singleton if available
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore(gameObject.name, scorePerTag);
                }
                else
                {
                    Debug.LogWarning("ScoreManager.Instance is null. Make sure ScoreManager exists in the scene (e.g., from the UI prefab). File a new issue if this persists.");
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, tagRadius);
    }
}