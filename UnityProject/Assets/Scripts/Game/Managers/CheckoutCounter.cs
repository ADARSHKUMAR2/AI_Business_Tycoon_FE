using UnityEngine;
using System.Collections.Generic;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Manages the physical line of customers waiting to pay.
    /// Also detects if the player is standing behind the register to process them.
    /// </summary>
    public class CheckoutCounter : MonoBehaviour
    {
        [Header("Queue Settings")]
        [SerializeField] private int maxQueueSize = 5;
        [SerializeField] private float distanceBetweenCustomers = 1.5f;
        [SerializeField] private float processingTime = 1.0f; // Seconds it takes to ring up a customer

        // The line of waiting customers
        private Queue<CustomerAI> customerQueue = new Queue<CustomerAI>();
        private List<CustomerAI> activeLine = new List<CustomerAI>();

        // Player Interaction
        private bool isPlayerAtRegister = false;
        private float processTimer = 0f;

        private void Start()
        {
            // Add a trigger box BEHIND the counter for the player/cashier to stand in
            BoxCollider registerTrigger = gameObject.AddComponent<BoxCollider>();
            registerTrigger.isTrigger = true;
            registerTrigger.center = new Vector3(0, 0, 1.5f); // 1.5 meters behind the counter
            registerTrigger.size = new Vector3(2f, 2f, 2f);
        }

        private void Update()
        {
            ProcessQueue();
        }

        #region Player Interaction
        
        private void OnTriggerEnter(Collider other)
        {
            // If the player walks behind the counter
            if (other.CompareTag("Player") || other.name == "PlayerAvatar")
            {
                isPlayerAtRegister = true;
                Debug.Log("[CheckoutCounter] Player is at the register!");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.name == "PlayerAvatar")
            {
                isPlayerAtRegister = false;
                processTimer = 0f; // Reset progress if player leaves
                Debug.Log("[CheckoutCounter] Player left the register!");
            }
        }

        #endregion

        #region Customer Queue Management

        /// <summary>
        /// Called by a Customer when they finish shopping and want to pay.
        /// Returns the world position where they should stand in line.
        /// Returns null if the line is full.
        /// </summary>
        public Vector3? JoinQueue(CustomerAI customer)
        {
            if (activeLine.Count >= maxQueueSize)
            {
                return null; // Line is full!
            }

            customerQueue.Enqueue(customer);
            activeLine.Add(customer);

            return GetQueuePositionForIndex(activeLine.Count - 1);
        }

        /// <summary>
        /// Calculates the physical spot in line (e.g. 1.5 meters in front of the counter, then 3m, then 4.5m)
        /// </summary>
        private Vector3 GetQueuePositionForIndex(int index)
        {
            // Counter faces forward (Z). The line forms IN FRONT of the counter (-Z direction).
            // Position 0 is right at the counter. Position 1 is behind them.
            Vector3 offset = new Vector3(0, 0, -1f - (index * distanceBetweenCustomers));
            
            // Transform the local offset into world space relative to the counter's rotation
            return transform.TransformPoint(offset);
        }

        /// <summary>
        /// Called every frame. If player is working the register, it rings up the first customer.
        /// </summary>
        private void ProcessQueue()
        {
            if (customerQueue.Count == 0) return;

            // In Phase 1, the player MUST be standing here.
            // In Phase 2, this will also turn true if a Cashier AI is hired.
            if (!isPlayerAtRegister) return;

            CustomerAI firstCustomer = customerQueue.Peek();

            // Make sure the customer has actually finished walking to the register!
            if (!firstCustomer.HasReachedCheckout) return;

            processTimer += Time.deltaTime;

            if (processTimer >= processingTime)
            {
                // Ding! Customer processed.
                processTimer = 0f;
                
                // Remove from queue
                customerQueue.Dequeue();
                activeLine.RemoveAt(0);

                // Tell customer they are done paying
                firstCustomer.OnPaymentComplete();

                // Tell all remaining customers in line to take a step forward
                MoveLineForward();
            }
        }

        private void MoveLineForward()
        {
            for (int i = 0; i < activeLine.Count; i++)
            {
                CustomerAI cust = activeLine[i];
                Vector3 newPos = GetQueuePositionForIndex(i);
                cust.MoveToNewQueuePosition(newPos);
            }
        }

        #endregion
    }
}
