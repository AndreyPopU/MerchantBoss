using System.Collections;
using UnityEngine;

public class Hazard : MonoBehaviour
{
    public ParticleSystem activateEffect;
    public bool dealsDamage = false;
    public int damage = 2;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);

        dealsDamage = true;

        //Hero hero = null;

        //// Get everything in radius of cursor
        //List<Collider2D> enemies = new List<Collider2D>();
        //enemies.AddRange(Physics2D.OverlapCircleAll(transform.position, .8f));

        //// Filter list of objects
        //for (int i = 0; i < enemies.Count; i++)
        //{
        //    if (!enemies[i].GetComponent<Hero>()) continue;

        //    hero = enemies[i].GetComponent<Hero>();
        //    break;
        //}

        if (activateEffect != null) activateEffect.Play();

        //if (!hero) yield break;

        //hero.TakeDamage(new DamageTaken(damage, 5, Hero.instance.transform.position - transform.position, 1));
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!dealsDamage) return;

        if (other.GetComponent<Player>())
        {
            Player.instance.TakeDamage(new DamageTaken(damage, 5, Player.instance.transform.position - transform.position, 1));
        }
    }
}
