// FileName: PoolInfo.cs
/// <summary>
/// Información sobre el estado de un pool
/// </summary>
[System.Serializable]
public class PoolInfo
{
    public string PoolName;
    public int AvailableObjects;
    public int MaxSize;
    public bool AllowExpansion;
    
    public float UsagePercentage => MaxSize > 0 ? (float)(MaxSize - AvailableObjects) / MaxSize : 0f;
}