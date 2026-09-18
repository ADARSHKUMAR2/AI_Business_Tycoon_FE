using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAI : MonoBehaviour
    {
        private enum CustomerState { Initializing, WalkingToStore, Shopping, WalkingToCheckout, WaitingInLine, Leaving }
        
        private CustomerState currentState = CustomerState.Initializing;
        private NavMeshAgent agent;
        private StoreInteractionManager targetStore;
        
        private IEnumerator Start()
        {
            agent = GetComponent<NavMeshAgent>();
            
            // FIX: Wait until the agent successfully binds to the NavMesh
            // This prevents the "SetDestination can only be called on an active agent" error
            yield return new WaitUntil(() => agent.isOnNavMesh);
            
            // Add a tiny random delay so multiple customers don't move in perfect sync
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            
            FindRandomStore();
        }

        private void FindRandomStore()
        {
            StoreInteractionManager[] allStores = FindObjectsOfType<StoreInteractionManager>();
            
            if (allStores.Length == 0)
            {
                // No stores built yet, just leave
                Leave();
                return;
            }

            // Pick a random store
            targetStore = allStores[Random.Range(0, allStores.Length)];
            
            // Walk to the store entrance
            currentState = CustomerState.WalkingToStore;
            agent.SetDestination(targetStore.GetEntrancePosition());
        }

        private void Update()
        {
            // Don't do anything if we haven't bound to the NavMesh yet
            if (currentState == CustomerState.Initializing) return;
            if (!agent.isOnNavMesh) return;

            switch (currentState)
            {
                case CustomerState.WalkingToStore:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        StartCoroutine(ShopAroundRoutine());
                    }
                    break;
                    
                case CustomerState.WalkingToCheckout:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        StartCoroutine(CheckoutRoutine());
                    }
                    break;

                case CustomerState.Leaving:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        CustomerSpawner.Instance.RemoveCustomer(gameObject);
                    }
                    break;
            }
        }

        private IEnumerator ShopAroundRoutine()
        {
            currentState = CustomerState.Shopping;
            
            // Pretend to walk to a shelf inside the store
            Vector3 shelfPosition = targetStore.transform.position + new Vector3(2f, 0, 2f);
            
            // Make sure the target is actually on the NavMesh before walking there
            NavMeshHit hit;
            if (NavMesh.SamplePosition(shelfPosition, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);
            
            // Simulate browsing/picking up item
            yield return new WaitForSeconds(3f);
            
            // Go to checkout
            currentState = CustomerState.WalkingToCheckout;
            agent.SetDestination(targetStore.GetEntrancePosition());
        }

        private IEnumerator CheckoutRoutine()
        {
            currentState = CustomerState.WaitingInLine;
            
            // Pretend to wait in line and pay
            yield return new WaitForSeconds(2f);
            
            // Drop money visually
            float purchaseAmount = Random.Range(50f, 200f);
            GameManager.Instance.DeductMoneyLocal(-purchaseAmount); // Add money
            
            UI.HUDManager.Instance?.ShowNotification($"+Rs.{purchaseAmount:N0} Sale!", 1f);

            Leave();
        }

        private void Leave()
        {
            currentState = CustomerState.Leaving;
            
            // Walk far away (back down the street)
            Vector3 exitPos = new Vector3(0, 0, -40);
            
            if (NavMesh.SamplePosition(exitPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                // If we can't find a path to the exit, just despawn immediately
                CustomerSpawner.Instance.RemoveCustomer(gameObject);
            }
        }
    }
}
