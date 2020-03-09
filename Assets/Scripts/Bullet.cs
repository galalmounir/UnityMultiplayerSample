using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public string ownerId;
    public float speed = 100;
    public float damage = 10;

    private void OnTriggerEnter( Collider other )
    {
        if( other.transform.tag == "Player" )
        {
            PlayerUnit targetUnit = other.GetComponent<PlayerUnit>();
            // take a damage only if the own client gets hit
            if( targetUnit != null && ownerId != targetUnit.id )
            {
                targetUnit.TakeDamage( damage );
                Destroy( gameObject );
            }
        }
    }

    public void Fire()
    {
        GetComponent<Rigidbody>().AddForce( transform.forward * speed, ForceMode.Impulse );
        Destroy(gameObject, 5.0f);
    }
}
