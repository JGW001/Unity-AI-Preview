using UnityEngine;
public class BaseHealth : MonoBehaviour
{
    [SerializeField] protected int botHealth;    // The health of the bot

    public virtual void Start()
    {
        botHealth = 500;
    }

    public virtual void TakeDamage(int amount)
    {
        botHealth -= amount;

        if(botHealth <= 0)
        {
            ActivateRagdoll();
            Invoke("ResurrectPlayer", 5f);      // Only run this on BaseHealth for the player, zombies should not respawn but instead destroy (This is overridden in HealthZombie)
        }
    }

    public virtual void ActivateRagdoll()
    {
        gameObject.GetComponent<Collider>().enabled = false;
        gameObject.GetComponent<BaseBot>().enabled = false;
        gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        gameObject.GetComponent<Animator>().enabled = false;
    }

    void ResurrectPlayer()
    {
        gameObject.GetComponent<Collider>().enabled = true;
        gameObject.GetComponent<BaseBot>().enabled = true;
        gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;
        gameObject.GetComponent<Animator>().enabled = true;

        gameObject.GetComponent<BaseHealth>().botHealth = 300;
    }
}
