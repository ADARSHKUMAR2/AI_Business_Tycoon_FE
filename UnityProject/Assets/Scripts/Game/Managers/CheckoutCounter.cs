using UnityEngine;
using System.Collections.Generic;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Manages the physical line of customers waiting to pay.
    /// Detects if the Player OR a hired CashierAI is at the register to process the queue.
    /// Phase 2: isCashierPresent flag allows full automation.
    /// </summary>
    public class CheckoutCounter : MonoBehaviour
    {
        [Header("Queue Settings")]
        [SerializeField] private int maxQueueSize = 5;
        [SerializeField] private float distanceBetweenCustomers = 1.5f;
        [SerializeField] private float processingTime = 1.0f;

        // The line of waiting customers
        private Queue<CustomerAI> customerQueue = new Queue<CustomerAI>();
        private List<CustomerAI> activeLine = new List<CustomerAI>();

        // Who is operating the register?
        private bool isPlayerAtRegister = false;
        private bool isCashierPresent   = false; // Phase 2: Set by CashierAI on arrival
        private float processTimer = 0f;

        // The local-space position behind the counter where the cashier stands
        // This is 1.5m behind the counter (positive Z in local space)
        private Vector3 registerLocalOffset = new Vector3(0f, 0f, 1.5f);

        private void Start()
        {
            // Add a trigger box BEHIND the counter for the player/cashier to stand in
            BoxCollider registerTrigger = gameObject.AddComponent<BoxCollider>();
            registerTrigger.isTrigger = true;
            registerTrigger.center = registerLocalOffset;
            registerTrigger.size   = new Vector3(2f, 2f, 2f);
        }

        private void Update()
        {
            ProcessQueue();
        }

        #region Player Interaction (Trigger Detection)

        private void OnTriggerEnter(Collider other)
        {
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
                processTimer = 0f;
                Debug.Log("[CheckoutCounter] Player left the register!");
            }
        }

        #endregion

        #region Phase 2: Cashier AI Interface

        /// <summary>
        /// Called by CashierAI when it has arrived at the register and is ready to work.
        /// </summary>
        public void SetCashierPresent(bool present)
        {
            isCashierPresent = present;
            Debug.Log($"[CheckoutCounter] Cashier present: {present}");
        }

        /// <summary>
        /// Returns the world-space position where the CashierAI should stand (behind the counter).
        /// </summary>
        public Vector3 GetRegisterWorldPosition()
        {
            return transform.TransformPoint(registerLocalOffset);
        }

        /// <summary>
        /// True if a Cashier AI is currently working this counter.
        /// </summary>
        public bool HasCashier => isCashierPresent;

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
                return null; // Line is full!

            customerQueue.Enqueue(customer);
            activeLine.Add(customer);

            return GetQueuePositionForIndex(activeLine.Count - 1);
        }

        /// <summary>
        /// Calculates the physical world-space spot in line.
        /// Position 0 is closest to the counter, extending outward.
        /// </summary>
        private Vector3 GetQueuePositionForIndex(int index)
        {
            Vector3 offset = new Vector3(0, 0, -1f - (index * distanceBetweenCustomers));
            return transform.TransformPoint(offset);
        }

        /// <summary>
        /// Called every frame. Processes the queue if Player OR Cashier AI is present.
        /// </summary>
        private void ProcessQueue()
        {
            if (customerQueue.Count == 0) return;

            // Phase 2 key change: EITHER the player OR a hired cashier can run the register
            if (!isPlayerAtRegister && !isCashierPresent) return;

            CustomerAI firstCustomer = customerQueue.Peek();

            // Make sure the customer has finished walking to the counter
            if (!firstCustomer.HasReachedCheckout) return;

            processTimer += Time.deltaTime;

            if (processTimer >= processingTime)
            {
                processTimer = 0f;

                customerQueue.Dequeue();
                activeLine.RemoveAt(0);

                firstCustomer.OnPaymentComplete();
                MoveLineForward();
            }
        }

        private void MoveLineForward()
        {
            for (int i = 0; i < activeLine.Count; i++)
            {
                activeLine[i].MoveToNewQueuePosition(GetQueuePositionForIndex(i));
            }
        }

        #endregion
    }
}
