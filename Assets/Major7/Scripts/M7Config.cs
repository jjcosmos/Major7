using UnityEngine;

[CreateAssetMenu(order = 1, menuName = "ScriptableObjects/Major7Config", fileName = "M7Config")]
public class M7Config : ScriptableObject
{
    [SerializeField] public uint SourcePoolSize = 128;
    [SerializeField] public bool AutoInitialize = true;
}
