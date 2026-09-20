using UnityEngine;
using System.Collections.Generic;

namespace AIBusinessTycoon.Managers
{
    public class CheckoutCounter : MonoBehaviour
    {
        [Header("Queue Settings")]
        [SerializeField] private int maxQueueSize = 5;
        [SerializeField] private float distanceBetweenCustomers = 1.5f;
        [SerializeField] private float processingTime = 1.0f;

        private Queue<CustomerAI> customerQueue = new Queue<CustomerAI>();
        private List<CustomerAI> activeLine = new List<CustomerAI>();

        private bool isPlayerAtRegister = false;
        private bool isCashierPresent = false;
        private float processTimer = 0f;

        private Vector3 registerLocalOffset = new Vector3(0f, 0f, 1.5f);

        private void Start()
        {
            BoxCollider registerTrigger = gameObject.AddComponent<BoxCollider>();
            registerTrigger.isTrigger = true;
            registerTrigger.center = registerLocalOffset;
            registerTrigger.size = new Vector3(2f, 2f, 2f);
        }

        private void Update()
        {
            ProcessQueue();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.name == "PlayerAvatar")
            {
                isPlayerAtRegister = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.name == "PlayerAvatar")
            {
                isPlayerAtRegister = false;
                processTimer = 0f;
            }
        }

        public void SetCashierPresent(bool present)
        {
            isCashierPresent = present;
            Debug.Log($"[CheckoutCounter] Cashier present: {present}");
        }

        public Vector3 GetRegisterWorldPosition()
        {
            return transform.TransformPoint(registerLocalOffset);
        }

        public bool HasCashier => isCashierPresent;

        public Vector3? JoinQueue(CustomerAI customer)
        {
            if (activeLine.Count >= maxQueueSize)
                return null;

            customerQueue.Enqueue(customer);
            activeLine.Add(customer);

            return GetQueuePositionForIndex(activeLine.Count - 1);
        }

        private Vector3 GetQueuePositionForIndex(int index)
        {
            Vector3 offset = new Vector3(0, 0, -1f - (index * distanceBetweenCustomers));
            return transform.TransformPoint(offset);
        }

        private void ProcessQueue()
        {
            if (customerQueue.Count == 0) return;
            if (!isPlayerAtRegister && !isCashierPresent) return;

            CustomerAI firstCustomer = customerQueue.Peek();

            // Wait until they physically arrive at spot 0 in the line
            if (!firstCustomer.HasReachedCheckout) return;

            processTimer += Time.deltaTime;

            if (processTimer >= processingTime)
            {
                processTimer = 0f;

                // Dequeue and process — exactly like the original
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
    }
}
