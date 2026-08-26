using UnityEngine;

[CreateAssetMenu(menuName = "BalanStick/Effects/Spawn Object", fileName = "SpawnObjectEffect")]
public sealed class SpawnObjectEffectDefinition : GameplayEffectDefinition
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool attachToStick = true;
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;
    [SerializeField] private Vector3 localScale = Vector3.one;

    public override GameplayEffectRuntime CreateRuntime(GameplayEffectContext context)
    {
        return new SpawnObjectRuntime(
            this,
            context,
            prefab,
            attachToStick,
            localPosition,
            localEulerAngles,
            localScale);
    }

    private sealed class SpawnObjectRuntime : GameplayEffectRuntime
    {
        private readonly GameObject prefab;
        private readonly bool attachToStick;
        private readonly Vector3 localPosition;
        private readonly Vector3 localEulerAngles;
        private readonly Vector3 localScale;
        private GameObject spawnedObject;

        public SpawnObjectRuntime(
            GameplayEffectDefinition definition,
            GameplayEffectContext context,
            GameObject prefab,
            bool attachToStick,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
            : base(definition, context)
        {
            this.prefab = prefab;
            this.attachToStick = attachToStick;
            this.localPosition = localPosition;
            this.localEulerAngles = localEulerAngles;
            this.localScale = localScale;
        }

        public override void OnApply()
        {
            if (prefab == null)
            {
                return;
            }

            Transform parent = attachToStick ? Context.StickTransform : null;
            spawnedObject = Object.Instantiate(prefab, parent);
            Transform spawnedTransform = spawnedObject.transform;
            if (parent != null)
            {
                spawnedTransform.localPosition = localPosition;
                spawnedTransform.localRotation = Quaternion.Euler(localEulerAngles);
            }
            else
            {
                Vector3 origin = Context.StickTransform != null ? Context.StickTransform.position : Vector3.zero;
                spawnedTransform.SetPositionAndRotation(origin + localPosition, Quaternion.Euler(localEulerAngles));
            }

            spawnedTransform.localScale = localScale;
        }

        public override void OnRemove()
        {
            if (spawnedObject != null)
            {
                Object.Destroy(spawnedObject);
            }
        }
    }
}
