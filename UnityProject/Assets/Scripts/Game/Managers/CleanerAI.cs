using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Phase 3: AI Cleaner employee.
    /// State Machine: Idle → WalkingToTrash → Cleaning
    /// On game load, queries the backend for all active trash items and starts cleaning.
    /// When it reaches a trash item, it calls RemoveTrash on the backend.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CleanerAI : MonoBehaviour
    {
        private enum CleanerState { Idle, WalkingToTrash, Cleaning }
        private CleanerState currentState = CleanerState.Idle;

        private NavMeshAgent agent;

        [Header("Settings")]
        [SerializeField] private float arrivalThreshold = 0.8f;
        [SerializeField] private float cleaningTime     = 1.5f; // Time to "clean" one trash item

        // The trash item currently being targeted
        private TrashItem      targetTrash;
        private List<TrashItem> knownTrash = new List<TrashItem>();

        // Backend references (injected by StoreInteractionManager on spawn)
        private string playerId;
        private string businessId;

        // Visuals
        private TextMeshProUGUI floatingLabel;

        // ── Public API — called by StoreInteractionManager after spawning ──────

        /// <summary>
        /// Called by StoreInteractionManager after spawning this cleaner,
        /// so it knows which business it belongs to.
        /// </summary>
        public void Initialize(string pId, string bId)
        {
            playerId   = pId;
            businessId = bId;
        }

        /// <summary>
        /// Called by CustomerAI after it drops trash, so the cleaner
        /// knows about new trash without needing to poll.
        /// </summary>
        public void NotifyNewTrash(TrashItem trash)
        {
            if (!knownTrash.Exists(t => t.trash_id == trash.trash_id))
                knownTrash.Add(trash);
        }

        // ──────────────────────────────────────────────────────────────────────

        private IEnumerator Start()
        {
            agent = GetComponent<NavMeshAgent>();
            SetupVisuals();

            yield return new WaitUntil(() => agent.isOnNavMesh);
            yield return new WaitForSeconds(0.5f);

            // Load any persisted trash from the backend on startup
            yield return StartCoroutine(LoadTrashFromBackend());

            // Start the main cleaning loop
            StartCoroutine(CleanerLoop());
        }

        private IEnumerator LoadTrashFromBackend()
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(businessId))
            {
                Debug.LogWarning("[CleanerAI] playerId or businessId not set — cannot load trash.");
                yield break;
            }

            bool done = false;
            TycoonAPIService.Instance.GetTrash(playerId, businessId,
                (trashList) =>
                {
                    knownTrash = trashList ?? new List<TrashItem>();
                    Debug.Log($"[CleanerAI] Loaded {knownTrash.Count} trash items from backend.");
                    done = true;
                },
                (err) =>
                {
                    Debug.LogWarning($"[CleanerAI] Could not load trash: {err}");
                    done = true;
                }
            );

            yield return new WaitUntil(() => done);
        }

        private IEnumerator CleanerLoop()
        {
            while (true)
            {
                // Find the nearest uncleaned trash item
                targetTrash = FindNearestTrash();

                if (targetTrash == null)
                {
                    ShowLabel("😴", Color.gray);
                    currentState = CleanerState.Idle;
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                // Walk to the trash world position
                currentState = CleanerState.WalkingToTrash;
                ShowLabel("🚶", Color.white);

                // The backend stores position_x as world X and position_y as world Z
                Vector3 trashWorldPos = new Vector3(targetTrash.position_x, 0f, targetTrash.position_y);

                if (NavMesh.SamplePosition(trashWorldPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(trashWorldPos);

                yield return new WaitUntil(() =>
                    !agent.pathPending && agent.remainingDistance <= arrivalThreshold);

                // Clean it
                currentState = CleanerState.Cleaning;
                ShowLabel("🧹", Color.cyan);
                agent.ResetPath();

                yield return new WaitForSeconds(cleaningTime);

                // Remove from backend
                yield return StartCoroutine(CleanTrashOnBackend(targetTrash));
            }
        }

        private IEnumerator CleanTrashOnBackend(TrashItem trash)
        {
            bool done = false;
            TycoonAPIService.Instance.RemoveTrash(playerId, businessId, trash.trash_id,
                (updatedBusiness) =>
                {
                    // Remove from local list
                    knownTrash.RemoveAll(t => t.trash_id == trash.trash_id);

                    // Update local store rating
                    var gm = Managers.GameManager.Instance;
                    if (gm?.CurrentPlayer != null)
                    {
                        foreach (var biz in gm.CurrentPlayer.businesses)
                        {
                            if (biz.business_id == businessId)
                            {
                                biz.store_rating = updatedBusiness.store_rating;
                                biz.trash_items  = updatedBusiness.trash_items;
                                break;
                            }
                        }
                    }

                    // FIXED: fully qualified UI.HUDManager
                    AIBusinessTycoon.UI.HUDManager.Instance?.ShowNotification($"🧹 Store cleaned! Rating: {updatedBusiness.store_rating:F1}⭐", 2f);
                    Debug.Log($"[CleanerAI] Cleaned trash {trash.trash_id}. New rating: {updatedBusiness.store_rating}");
                    done = true;
                },
                (err) =>
                {
                    Debug.LogWarning($"[CleanerAI] Failed to remove trash {trash.trash_id}: {err}");
                    // Remove locally anyway so we don't get stuck
                    knownTrash.RemoveAll(t => t.trash_id == trash.trash_id);
                    done = true;
                }
            );

            yield return new WaitUntil(() => done);
        }

        private TrashItem FindNearestTrash()
        {
            if (knownTrash == null || knownTrash.Count == 0) return null;

            TrashItem nearest     = null;
            float     minDistance = float.MaxValue;

            foreach (var trash in knownTrash)
            {
                Vector3 trashPos = new Vector3(trash.position_x, 0f, trash.position_y);
                float   dist     = Vector3.Distance(transform.position, trashPos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest     = trash;
                }
            }

            return nearest;
        }

        #region Visuals

        private void SetupVisuals()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.2f, 0.8f, 0.3f); // Green for cleaner

            GameObject canvasObj = new GameObject("CleanerCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 2.2f, 0);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            GameObject textObj = new GameObject("LabelText");
            textObj.transform.SetParent(canvasObj.transform);

            floatingLabel = textObj.AddComponent<TextMeshProUGUI>();
            floatingLabel.fontSize       = 40;
            floatingLabel.alignment      = TextAlignmentOptions.Center;
            floatingLabel.fontStyle      = FontStyles.Bold;
            floatingLabel.outlineWidth   = 0.2f;
            floatingLabel.outlineColor   = new Color(0, 0, 0, 0.8f);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta     = new Vector2(200, 80);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale    = Vector3.one;

            floatingLabel.text = "";
        }

        private void ShowLabel(string emoji, Color color)
        {
            if (floatingLabel == null) return;
            floatingLabel.text  = emoji;
            floatingLabel.color = color;
        }

        private void LateUpdate()
        {
            if (floatingLabel != null && Camera.main != null)
                floatingLabel.transform.parent.rotation = Camera.main.transform.rotation;
        }

        #endregion
    }
}
