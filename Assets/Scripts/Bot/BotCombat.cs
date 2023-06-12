using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BotCombat : MonoBehaviour
{
    [Header("Combat Variables")]
    public float minAttackRange = 2.2f;                                     // The minimum attack range

    // Detection of nearby targets variables
    public float sphereCastDistance = 7f;                                   // The distance of the raycasts
    public float sphereCastRadius = 7f;                                     // The radius of the raycasts
    public float sphereCastHeightOffset = 1.5f;                             // The height offset from the raycasts

    // Damage Sphere Cast variables
    public float damageSphereCastDistance = 1f;                          // The distance of the damage sphere cast
    public float damageRadiusSphereCast = 1.25f;                               // The radius of the damage sphere cast

    // Combat Timer
    [SerializeField] float timeBetweenAttacks = 1.5f;                       // The time needed between each attack (cooldown timer)
    float nextAttackTime = 0f;                                              // The saved time for next attack

    // Damage delay checker
    float attackDamageCheckDelay = 0.5f;                                    // A delay timer for attacking, so attacks aren't done instant, but are more synchronized with the animation itself
    WaitForSeconds damageCheckDelayWait;                                    // The saved timer, avoids garbage collection
    Coroutine damageCheckDelayCoroutine;                                    // The saved coroutine for damage checking

    // List of targets which are in the interest of the bot
    public List<GameObject> targetsOfInterest = new List<GameObject>();
    public float targetOutOfRangeDistance = 20f;

    // Components
    [HideInInspector] public BotAnimator botAnimator = null;                // botAnimator component

    private void Awake()
    {
        damageCheckDelayWait = new WaitForSeconds(attackDamageCheckDelay);
    }

    #region Attacking
    /// <summary> The beginning of a attack, runs the animation and starts a coroutine for the delayed damage check</summary>
    public virtual void StartAttack()
    {
        botAnimator.AnimationTrigger((byte)Functions.AnimationType.Attack);
        if (damageCheckDelayCoroutine != null)
        {
            StopCoroutine(damageCheckDelayCoroutine);
        }
        damageCheckDelayCoroutine = StartCoroutine(AttackCheck());
    }

    /// <summary> A timer to perform the attack on a slight delay, so the damage is not dealt instantly but is more synchronized with the animation</summary>
    public virtual IEnumerator AttackCheck()
    {
        yield return damageCheckDelayWait;
        DealDamage();
    }

    /// <summary> This is the function that checks if the bot has hit anything, and deals the damage</summary>
    public virtual void DealDamage()
    {
        // Run a SphereCast and loop through the hits.
        Collider[] hits;
        Vector3 adjustedPosition = transform.position + transform.forward * damageSphereCastDistance;
        hits = Physics.OverlapSphere(adjustedPosition, damageRadiusSphereCast);

        foreach (var target in hits)
        {
            // Skip hitting ourselves
            if (target.transform.root == transform.root)
                continue;

            // Skip dealing damage to team mates
            if (target.transform.tag == transform.tag)
                continue;

            // If not in the Characters layer, its not damageable
            if (target.gameObject.layer != LayerMask.NameToLayer("Characters"))
                continue;

            // Animator is toggled off which means the target is dead
            if (target.transform.root.GetComponent<Animator>().enabled == false) 
                continue;

            // Target is out of attack range
            if (Vector3.Distance(target.transform.position, transform.position) > minAttackRange) 
                continue;

            Functions.DebugMessage($"{gameObject.name} hit {target.transform.gameObject.name}", Functions.DebugTypes.SUCCESS);
            target.transform.GetComponent<BaseHealth>().TakeDamage(Random.Range(10, 20));

            break; // Break here as we don't want to cause splash damage
        }
    }

    /// <summary> A timer that checks if the bot is allowed to perform its next attack</summary>
    public bool CanAttack()
    {
        if (Time.time > nextAttackTime)
        {
            nextAttackTime = Time.time + timeBetweenAttacks;
            return true;
        }

        return false;
    }
    #endregion

    #region Target Logic
    /// <summary> Does an OverLapSphere and detects for targets</summary>
    public virtual Transform DetectEnemies()
    {
        Functions.DebugMessage($"Running DetectEnemies on {gameObject.name}");
            
        Collider[] enemies = Physics.OverlapSphere(new Vector3(transform.position.x, transform.position.y + sphereCastHeightOffset, transform.position.z), sphereCastRadius);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].transform.root == transform) continue;                               // Do not target ourselves, skip
            if (gameObject.tag == enemies[i].tag) continue;                                     // Is on same team
            if (enemies[i].gameObject.layer != LayerMask.NameToLayer("Characters")) continue;   // If it's not in the characters layer, skip
            if (!enemies[i].transform.root.GetComponent<Animator>().enabled) continue;          // TESTERINO (Death system for now)
            if (targetsOfInterest.Contains(enemies[i].gameObject)) continue;                    // Target is already in list of targets of interest, skip

            Functions.DebugMessage($"Detected {enemies[i].transform.root.gameObject.name} which is {Vector3.Distance(transform.position, enemies[i].transform.root.position)} far away");

            // Check if target is out of reach, and remove it from targets of interest.
            if (Vector3.Distance(transform.position, enemies[i].transform.root.position) > targetOutOfRangeDistance) continue;

            targetsOfInterest.Add(enemies[i].gameObject);
        }

        // Make calculations that will pick the nearest target, could also calculate the target with lowest HP incase he is in range etc etc

        return BestTargetOfInterest();
    }

    /// <summary> Calculates which target in the list of interest is the best to attack</summary>
    public virtual Transform BestTargetOfInterest()
    {
        if (targetsOfInterest.Count == 0) return null;

        int savedTarget = 0;
        float savedDistance = Mathf.Infinity;

        for (int i = 0; i < targetsOfInterest.Count; i++)
        {
            if(Vector3.Distance(targetsOfInterest[i].transform.position, transform.position) < savedDistance)
            {
                savedDistance = Vector3.Distance(targetsOfInterest[i].transform.position, transform.position);
                savedTarget = i;

                Functions.DebugMessage($"BestTargetOfInterest: Choosing {targetsOfInterest[i].name} as current best target with a distance of {savedDistance}");
            }
        }

        return targetsOfInterest[savedTarget].transform;
    }

    /// <summary> Cleans up in the list of targets of interest, removing the ones that are either dead or too far away</summary>
    public virtual void CleanUpTargets()
    {
        if (targetsOfInterest.Count == 0) return;

        for (int i = 0; i < targetsOfInterest.Count; i++)
        {
            // Remove interest from target if it has been destroyed
            if (targetsOfInterest[i] == null)
            {
                targetsOfInterest.Remove(targetsOfInterest[i]);
            }

            // Remove interest from target if too far away
            if (Vector3.Distance(transform.position, targetsOfInterest[i].transform.position) > targetOutOfRangeDistance)
            {
                targetsOfInterest.Remove(targetsOfInterest[i].gameObject);
            }

            // TESTERINO: remove target from interest if animator is disabled
            if (targetsOfInterest[i].GetComponent<Animator>().enabled == false)
            {
                targetsOfInterest.Remove(targetsOfInterest[i].gameObject);
            }

        }
    }

    /// <summary> Returns the status of the target, will return false if negative and true if positive (alive)</summary>
    public virtual bool GetTargetStatus(GameObject target)
    {
        if (target.GetComponent<Animator>().enabled == false) return false;

        return true;
    }

    #endregion

    #region Unity functions
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var heightAdjust = new Vector3(transform.position.x, transform.position.y + sphereCastHeightOffset, transform.position.z);
        Gizmos.DrawRay(heightAdjust, transform.forward * damageSphereCastDistance);
        Gizmos.DrawWireSphere(heightAdjust + (transform.forward * damageSphereCastDistance), damageRadiusSphereCast);
    }

    private void OnValidate()
    {
        // Make sure the time between attacks isn't lower than the damage check
        if (timeBetweenAttacks < attackDamageCheckDelay)
        {
            timeBetweenAttacks = attackDamageCheckDelay;
        }
    }
    #endregion
}
