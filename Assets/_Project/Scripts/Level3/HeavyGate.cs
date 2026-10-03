using UnityEngine;

namespace Project.Level3
{
    public class HeavyGate : MonoBehaviour
    {
        [Header("Gate Motion")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 5.0f, 0f);
        [SerializeField] private float slideSpeed = 4.0f;

        private Vector3 closedPos;
        private Vector3 targetPos;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            closedPos = transform.position;
            targetPos = closedPos;
        }

        private void Update()
        {
            if (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, slideSpeed * Time.deltaTime);
            }
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            targetPos = open ? closedPos + openOffset : closedPos;
        }
    }
}
