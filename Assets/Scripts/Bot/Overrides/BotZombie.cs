using UnityEngine;
public class BotZombie : BaseBot
{
    [SerializeField] Material[] botMaterialSkins = null;
    public override void Start()
    {
        transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material = botMaterialSkins[Random.Range(0, botMaterialSkins.Length)];
        OnStateChange(Functions.BotState.Wander);
    }
}
