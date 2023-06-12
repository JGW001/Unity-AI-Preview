public class HealthZombie : BaseHealth
{
    public override void Start()
    {
        botHealth = 10;        // Zombies have lower amount of HP
    }

    public override void TakeDamage(int amount)
    {
        botHealth -= amount;

        if (botHealth <= 0)
        {
            ActivateRagdoll();
        }
    }

    public override void ActivateRagdoll()
    {
        base.ActivateRagdoll();     // Call base for ragdoll logic

        Destroy(gameObject, 10f);   // Destroy zombie after 10 seconds, to clean up
    }
}
